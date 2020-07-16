using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using DatabaseManager.Server.Entities;
using DatabaseManager.Server.Helpers;
using DatabaseManager.Shared;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DatabaseManager.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DataQCController : ControllerBase
    {
        private readonly string connectionString;
        private readonly string container = "sources";
        private readonly IWebHostEnvironment _env;
        private List<DataAccessDef> _accessDefs;
        private DataAccessDef _indexAccessDef;
        private QcFlags qcFlags;
        private SqlDataAdapter indexAdapter;
        private DataTable indexTable;
        private SqlConnection sqlCn = null;

        public DataQCController(IConfiguration configuration, IWebHostEnvironment env)
        {
            connectionString = configuration.GetConnectionString("AzureStorageConnection");
            _env = env;
            _accessDefs = Common.GetDataAccessDefinition(_env);
            _indexAccessDef = _accessDefs.First(x => x.DataType == "Index");
            qcFlags = new QcFlags();
        }

        [HttpGet("{source}")]
        public async Task<ActionResult<List<QcResult>>> Get(string source)
        {
            ConnectParameters connector = Common.GetConnectParameters(connectionString, container, source);
            if (connector == null) return BadRequest();
            DbUtilities dbConn = new DbUtilities();
            List<QcResult> qcResults = new List<QcResult>();
            try
            {
                dbConn.OpenConnection(connector);
                qcResults = GetQcResult(dbConn);
            }
            catch (Exception)
            {
                return BadRequest();
            }

            dbConn.CloseConnection();
            return qcResults;
        }

        [HttpPost]
        public async Task<ActionResult<string>> ExecuteRule(DataQCParameters qcParams)
        {
            try
            {
                if (qcParams == null) return BadRequest();
                ConnectParameters connector = Common.GetConnectParameters(connectionString, container, qcParams.DataConnector);
                if (connector == null) return BadRequest();
                DbUtilities dbConn = new DbUtilities();
                dbConn.OpenConnection(connector);

                if (qcParams.ClearQCFlags)
                {
                    GetQCFlags(connector.ConnectionString, qcParams.ClearQCFlags, "");
                    SaveQCFlags();
                }

                RuleModel rule = GetRule(dbConn, qcParams.RuleId);
                GetQCFlags(connector.ConnectionString, qcParams.ClearQCFlags, rule.DataType);
                QualityCheckDataType(dbConn, rule, connector);
                dbConn.CloseConnection();
                SaveQCFlags();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.ToString());
            }

            return Ok($"OK");
        }

        private void GetQCFlags(string connectionString, bool clearQcFlags, string dataType)
        {
            DataAccessDef ruleAccessDef = _accessDefs.First(x => x.DataType == "Index");
            string select = ruleAccessDef.Select;
            if (!string.IsNullOrEmpty(dataType))
            {
                select = select + $" where DATATYPE = '{dataType}'";
            }
            sqlCn = new SqlConnection(connectionString);
            indexAdapter = new SqlDataAdapter();
            indexAdapter.SelectCommand = new SqlCommand(select, sqlCn);
            indexTable = new DataTable();
            indexAdapter.Fill(indexTable);

            foreach (DataRow row in indexTable.Rows)
            {
                int idx = Convert.ToInt32(row["INDEXID"]);
                if (clearQcFlags)
                {
                    qcFlags[idx] = "";
                }
                else
                {
                    qcFlags[idx] = row["QC_STRING"].ToString();
                }
                
            }
        }

        private void SaveQCFlags()
        {
            foreach (DataRow row in indexTable.Rows)
            {
                int indexID = Convert.ToInt32(row["INDEXID"]);
                row["QC_STRING"] = qcFlags[indexID];
            }

            string upd = @"update pdo_qc_index set QC_STRING = @qc_string where INDEXID = @id";
            SqlCommand cmd = new SqlCommand(upd, sqlCn);
            cmd.Parameters.Add("@qc_string", SqlDbType.NVarChar, 400, "QC_STRING");
            SqlParameter parm = cmd.Parameters.Add("@id", SqlDbType.Int, 4, "INDEXID");
            parm.SourceVersion = DataRowVersion.Original;

            indexAdapter.UpdateCommand = cmd;
            indexAdapter.Update(indexTable);
        }

        private void QualityCheckDataType(DbUtilities dbConn, RuleModel rule, ConnectParameters connector)
        {
            QcRuleSetup qcSetup = new QcRuleSetup();
            qcSetup.Database = connector.Database;
            qcSetup.DatabasePassword = connector.DatabasePassword;
            qcSetup.DatabaseServer = connector.DatabaseServer;
            qcSetup.DatabaseUser = connector.DatabaseUser;
            DataAccessDef ruleAccessDef = _accessDefs.First(x => x.DataType == "Index");
            //string sql = ruleAccessDef.Select;
            //string query = $" where DATATYPE = '{rule.DataType}'";
            //DataTable idx = dbConn.GetDataTable(sql, query);
            string ruleFilter = rule.RuleFilter;
            string jsonRules = JsonConvert.SerializeObject(rule);
            qcSetup.RuleObject = jsonRules;
            foreach (DataRow idxRow in indexTable.Rows)
            {
                string jsonData = idxRow["JSONDATAOBJECT"].ToString();
                qcSetup.IndexId = Convert.ToInt32(idxRow["INDEXID"]);
                qcSetup.IndexNode = idxRow["Text_IndexNode"].ToString();
                string qcStr = qcFlags[qcSetup.IndexId];
                qcSetup.DataObject = jsonData;
                if (!Filter(jsonData, ruleFilter)) ProcessQcRule(qcSetup, rule);
            }
        }

        private void ProcessQcRule(QcRuleSetup qcSetup, RuleModel rule)
        {
            var jsonString = JsonConvert.SerializeObject(qcSetup);
            var content = new StringContent(jsonString, Encoding.UTF8, "application/json");
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    using (HttpResponseMessage response = client.PostAsync(rule.RuleFunction, content).Result)
                    {
                        using (HttpContent respContent = response.Content)
                        {
                            var tr = respContent.ReadAsStringAsync().Result;
                            var azureResponse = JsonConvert.DeserializeObject(tr);
                            if (azureResponse.ToString() == "Failed")
                            {
                                string qcStr = qcFlags[qcSetup.IndexId];
                                qcStr = qcStr + rule.RuleKey + ";";
                                qcFlags[qcSetup.IndexId] = qcStr;
                            }
                                
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Exception error = new Exception("DataQcWithProgressBar: Problems with URL: ", ex);
                throw error;
            }
        }

        private RuleModel GetRule(DbUtilities dbConn, int id)
        {
            List<RuleModel> rules = new List<RuleModel>();
            DataAccessDef ruleAccessDef = _accessDefs.First(x => x.DataType == "Rules");
            string sql = ruleAccessDef.Select;
            string query = $" where Id = {id}";
            DataTable dt = dbConn.GetDataTable(sql, query);
            string jsonString = JsonConvert.SerializeObject(dt);
            rules = JsonConvert.DeserializeObject<List<RuleModel>>(jsonString);
            RuleModel rule = rules.First();

            DataAccessDef functionAccessDef = _accessDefs.First(x => x.DataType == "Functions");
            sql = functionAccessDef.Select;
            query = $" where FunctionName = '{rule.RuleFunction}'";
            dt = dbConn.GetDataTable(sql, query);

            string functionURL = dt.Rows[0]["FunctionUrl"].ToString();
            string functionKey = dt.Rows[0]["FunctionKey"].ToString();
            if (!string.IsNullOrEmpty(functionKey)) functionKey = "?code=" + functionKey;
            rule.RuleFunction = functionURL + functionKey;
            return rule;
        }

        private List<QcResult> GetQcResult(DbUtilities dbConn)
        {
            List<QcResult> qcResult = new List<QcResult>();
            DataAccessDef ruleAccessDef = _accessDefs.First(x => x.DataType == "Rules");
            string sql = ruleAccessDef.Select;
            string query = " where Active = 'Y' and RuleType != 'Predictions'";
            DataTable dt = dbConn.GetDataTable(sql, query);
            string jsonString = JsonConvert.SerializeObject(dt);
            qcResult = JsonConvert.DeserializeObject<List<QcResult>>(jsonString);
            foreach (QcResult qcItem in qcResult)
            {
                sql = _indexAccessDef.Select;
                query = $" where QC_STRING like '%{qcItem.RuleKey};%'";
                DataTable ft = dbConn.GetDataTable(sql, query);
                qcItem.Failures = ft.Rows.Count;
            }

            return qcResult;
        }

        private Boolean Filter(string jsonData, string ruleFilter)
        {
            Boolean filter = false;
            if (!string.IsNullOrEmpty(ruleFilter))
            {
                string[] filterValues = ruleFilter.Split('=');
                if (filterValues.Length == 2)
                {
                    JObject json = JObject.Parse(jsonData);
                    string dataAttribute = filterValues[0].Trim();
                    string dataValue = filterValues[1].Trim();
                    if (json[dataAttribute].ToString() != dataValue) filter = true;
                }
                else
                {
                    filter = true;
                }
            }
            return filter;
        }
    }
}
