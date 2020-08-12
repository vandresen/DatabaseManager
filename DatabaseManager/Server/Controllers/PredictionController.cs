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
    public class PredictionController : ControllerBase
    {
        private string connectionString;
        private readonly string container = "sources";
        private readonly IWebHostEnvironment _env;
        List<DataAccessDef> _accessDefs;
        DataAccessDef _indexAccessDef;
        private SqlConnection sqlCn = null;
        private SqlDataAdapter indexAdapter;
        private DataTable indexTable;
        private QcFlags qcFlags;
        private static HttpClient Client = new HttpClient();

        public PredictionController(IConfiguration configuration, IWebHostEnvironment env)
        {
            connectionString = configuration.GetConnectionString("AzureStorageConnection");
            _env = env;
            _accessDefs = Common.GetDataAccessDefinition(_env);
            _indexAccessDef = _accessDefs.First(x => x.DataType == "Index");
            qcFlags = new QcFlags();
        }

        [HttpGet("{source}")]
        public async Task<ActionResult<List<PredictionCorrection>>> Get(string source)
        {
            string tmpConnString = Request.Headers["AzureStorageConnection"];
            if (!string.IsNullOrEmpty(tmpConnString)) connectionString = tmpConnString;
            if (string.IsNullOrEmpty(connectionString)) return NotFound("Connection string is not set");

            ConnectParameters connector = Common.GetConnectParameters(connectionString, container, source);
            if (connector == null) return BadRequest();
            DbUtilities dbConn = new DbUtilities();
            List<PredictionCorrection> predictionResuls = new List<PredictionCorrection>();
            try
            {
                dbConn.OpenConnection(connector);
                predictionResuls = GetPredictionCorrections(dbConn);
            }
            catch (Exception)
            {
                return BadRequest();
            }

            dbConn.CloseConnection();
            return predictionResuls;
        }

        [HttpPost]
        public async Task<ActionResult<string>> ExecutePrediction(PredictionParameters predictionParams)
        {
            string tmpConnString = Request.Headers["AzureStorageConnection"];
            if (!string.IsNullOrEmpty(tmpConnString)) connectionString = tmpConnString;
            if (string.IsNullOrEmpty(connectionString)) return NotFound("Connection string is not set");
            try
            {
                if (predictionParams == null) return BadRequest();
                ConnectParameters connector = Common.GetConnectParameters(connectionString, container, predictionParams.DataConnector);
                if (connector == null) return BadRequest();
                DbUtilities dbConn = new DbUtilities();
                dbConn.OpenConnection(connector);

                RuleModel rule = Common.GetRule(dbConn, predictionParams.PredictionId, _accessDefs);
                GetQCFlags(connector.ConnectionString, rule.DataType, rule.FailRule);
                MakePredictions(rule, connector, dbConn);
                dbConn.CloseConnection();
                SaveQCFlags();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.ToString());
            }

            return Ok($"OK");
        }

        private void MakePredictions(RuleModel rule, ConnectParameters connector, DbUtilities dbConn)
        {
            QcRuleSetup qcSetup = new QcRuleSetup();
            qcSetup.Database = connector.Database;
            qcSetup.DatabasePassword = connector.DatabasePassword;
            qcSetup.DatabaseServer = connector.DatabaseServer;
            qcSetup.DatabaseUser = connector.DatabaseUser;
            string jsonRules = JsonConvert.SerializeObject(rule);
            qcSetup.RuleObject = jsonRules;
            string predictionURL = rule.RuleFunction;
            foreach (DataRow idxRow in indexTable.Rows)
            {
                string jsonData = idxRow["JSONDATAOBJECT"].ToString();
                if (!string.IsNullOrEmpty(jsonData))
                {
                    qcSetup.IndexId = Convert.ToInt32(idxRow["INDEXID"]);
                    qcSetup.IndexNode = idxRow["Text_IndexNode"].ToString();
                    string qcStr = qcFlags[qcSetup.IndexId];
                    qcSetup.DataObject = jsonData;
                    ProcessPrediction(qcSetup, predictionURL, rule, dbConn);
                }
            }
        }

        private void ProcessPrediction(QcRuleSetup qcSetup, string predictionURL, RuleModel rule, DbUtilities dbConn)
        {
            try
            {
                var jsonString = JsonConvert.SerializeObject(qcSetup);
                var content = new StringContent(jsonString, Encoding.UTF8, "application/json");
                HttpResponseMessage response = Client.PostAsync(predictionURL, content).Result;
                using (HttpContent respContent = response.Content)
                {
                    string tr = respContent.ReadAsStringAsync().Result;
                    PredictionResult result = JsonConvert.DeserializeObject<PredictionResult>(tr);
                    if (result.Status == "Passed")
                    {
                        string qcStr = qcFlags[result.IndexId];
                        string failRule = rule.FailRule + ";";
                        string pCode = rule.RuleKey + ";";
                        if (result.SaveType == "Delete")
                        {
                            qcStr = pCode;
                        }
                        else
                        {
                            qcStr = qcStr.Replace(failRule, pCode);
                        }
                        qcFlags[result.IndexId] = qcStr;
                        SavePrediction(result, dbConn, qcStr);
                    }
                    else
                    {
                        //FailedPredictions++;
                    }
                }
            }
            catch (Exception Ex)
            {
                //Trace.TraceInformation("ProcessDataObject: Problems with URL");
            }
        }

        private void SavePrediction(PredictionResult result, DbUtilities dbConn, string qcStr)
        {
            if (result.SaveType == "Update")
            {
                UpdateAction(result, dbConn, qcStr);
            }
            else if (result.SaveType == "Insert")
            {
                //InsertAction(result, dbDAL);
            }
            else if (result.SaveType == "Delete")
            {
                DeleteAction(result, dbConn, qcStr);
            }
            else
            {
                //Trace.TraceWarning($"Save type {result.SaveType} is not supported");
            }
        }

        private void UpdateAction(PredictionResult result, DbUtilities dbConn, string qcStr)
        {
            DataAccessDef ruleAccessDef = _accessDefs.First(x => x.DataType == "Index");
            string select = ruleAccessDef.Select;
            //string idxTable = GetTable(select);
            string idxQuery = $" where INDEXID = {result.IndexId}";
            DataTable idx = dbConn.GetDataTable(select, idxQuery);
            if (idx.Rows.Count == 1)
            {
                string condition = $"INDEXID={result.IndexId}";
                var rows = indexTable.Select(condition);
                rows[0]["JSONDATAOBJECT"] = result.DataObject;
                rows[0]["QC_STRING"] = qcStr;
                indexTable.AcceptChanges();

                string jsonDataObject = result.DataObject;
                JObject dataObject = JObject.Parse(jsonDataObject);
                dataObject["ROW_CHANGED_BY"] = Environment.UserName;
                jsonDataObject = dataObject.ToString();
                jsonDataObject = Common.SetJsonDataObjectDate(jsonDataObject, "ROW_CHANGED_DATE");
                string dataType = idx.Rows[0]["DATATYPE"].ToString();
                dbConn.UpdateDataObject(jsonDataObject, dataType);
            }
            else
            {
                //Trace.TraceWarning("Cannot find data key during update");
            }
        }

        private void DeleteAction(PredictionResult result, DbUtilities dbDAL, string qcStr)
        {
            DataAccessDef ruleAccessDef = _accessDefs.First(x => x.DataType == "Index");
            string select = ruleAccessDef.Select;
            string idxTable = GetTable(select);
            string idxQuery = $" where INDEXID = {result.IndexId}";
            DataTable idx = dbDAL.GetDataTable(select, idxQuery);
            if (idx.Rows.Count == 1)
            {
                string condition = $"INDEXID={result.IndexId}";
                var rows = indexTable.Select(condition);
                rows[0]["JSONDATAOBJECT"] = "";
                rows[0]["QC_STRING"] = qcStr;
                indexTable.AcceptChanges();

                string dataType = idx.Rows[0]["DATATYPE"].ToString();
                string dataKey = idx.Rows[0]["DATAKEY"].ToString();
                ruleAccessDef = _accessDefs.First(x => x.DataType == dataType);
                select = ruleAccessDef.Select;
                string dataTable = GetTable(select);
                string dataQuery = "where " + dataKey;
                dbDAL.DBDelete(dataTable, dataQuery);

                //dbDAL.DBDelete(idxTable, idxQuery);
            }
            else
            {
                //Trace.TraceWarning("Cannot find data key during update");
            }

        }

        private void GetQCFlags(string connectionString, string dataType, string qcRule)
        {
            DataAccessDef ruleAccessDef = _accessDefs.First(x => x.DataType == "Index");
            string select = ruleAccessDef.Select;
            if (!string.IsNullOrEmpty(dataType))
            {
                select = select + $" where DATATYPE = '{dataType}' and QC_STRING like '%{qcRule}%'";
            }
            sqlCn = new SqlConnection(connectionString);
            indexAdapter = new SqlDataAdapter();
            indexAdapter.SelectCommand = new SqlCommand(select, sqlCn);
            indexTable = new DataTable();
            indexAdapter.Fill(indexTable);

            foreach (DataRow row in indexTable.Rows)
            {
                int idx = Convert.ToInt32(row["INDEXID"]);
                qcFlags[idx] = row["QC_STRING"].ToString();
            }
        }

        private List<PredictionCorrection> GetPredictionCorrections(DbUtilities dbConn)
        {
            List<PredictionCorrection> predictionResult = new List<PredictionCorrection>();
            DataAccessDef ruleAccessDef = _accessDefs.First(x => x.DataType == "Rules");
            string sql = ruleAccessDef.Select;
            string query = " where Active = 'Y' and RuleType = 'Predictions' order by PredictionOrder";
            DataTable dt = dbConn.GetDataTable(sql, query);
            string jsonString = JsonConvert.SerializeObject(dt);
            predictionResult = JsonConvert.DeserializeObject<List<PredictionCorrection>>(jsonString);

            foreach (PredictionCorrection predItem in predictionResult)
            {
                sql = _indexAccessDef.Select;
                query = $" where QC_STRING like '%{predItem.RuleKey};%'";
                DataTable ft = dbConn.GetDataTable(sql, query);
                predItem.NumberOfCorrections = ft.Rows.Count;
            }

            return predictionResult;
        }

        private void SaveQCFlags()
        {
            foreach (DataRow row in indexTable.Rows)
            {
                int indexID = Convert.ToInt32(row["INDEXID"]);
                row["QC_STRING"] = qcFlags[indexID];
            }

            string upd = @"update pdo_qc_index set QC_STRING = @qc_string, JSONDATAOBJECT = @jsondataobject where INDEXID = @id";
            SqlCommand cmd = new SqlCommand(upd, sqlCn);
            cmd.Parameters.Add("@qc_string", SqlDbType.NVarChar, 400, "QC_STRING");
            cmd.Parameters.Add("@jsondataobject", SqlDbType.NVarChar, -1, "JSONDATAOBJECT");
            SqlParameter parm = cmd.Parameters.Add("@id", SqlDbType.Int, 4, "INDEXID");
            parm.SourceVersion = DataRowVersion.Original;

            indexAdapter.UpdateCommand = cmd;
            indexAdapter.Update(indexTable);
        }

        private string GetTable(string select)
        {
            select = select.ToUpper();
            int from = select.IndexOf(" FROM ") + 6;
            string table = select.Substring(from);
            return table;
        }
    }
}
