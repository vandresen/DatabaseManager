using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using DatabaseManager.Server.Entities;
using DatabaseManager.Server.Extensions;
using DatabaseManager.Server.Helpers;
using DatabaseManager.Server.Services;
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
        private string connectionString;
        private readonly string container = "sources";
        private readonly IFileStorageService fileStorageService;
        private readonly IWebHostEnvironment _env;
        private List<DataAccessDef> _accessDefs;
        private DataAccessDef _indexAccessDef;
        private ManageIndexTable manageQCFlags;


        public DataQCController(IConfiguration configuration,
            IFileStorageService fileStorageService,
            IWebHostEnvironment env)
        {
            connectionString = configuration.GetConnectionString("AzureStorageConnection");
            this.fileStorageService = fileStorageService;
            _env = env;
        }

        [HttpGet("{source}")]
        public async Task<ActionResult<List<QcResult>>> Get(string source)
        {
            string tmpConnString = Request.Headers["AzureStorageConnection"];
            if (!string.IsNullOrEmpty(tmpConnString)) connectionString = tmpConnString;
            if (string.IsNullOrEmpty(connectionString)) return NotFound("Connection string is not set");

            ConnectParameters connector = Common.GetConnectParameters(connectionString, container, source);
            if (connector == null) return BadRequest();
            DbUtilities dbConn = new DbUtilities();
            List<QcResult> qcResults = new List<QcResult>();
            try
            {
                fileStorageService.SetConnectionString(tmpConnString);
                string accessJson = await fileStorageService.ReadFile("connectdefinition", "PPDMDataAccess.json");
                _accessDefs = JsonConvert.DeserializeObject<List<DataAccessDef>>(accessJson);
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

        [HttpGet("{source}/{id}")]
        public async Task<ActionResult<string>> GetFailures(string source, int id)
        {
            string tmpConnString = Request.Headers["AzureStorageConnection"];
            if (!string.IsNullOrEmpty(tmpConnString)) connectionString = tmpConnString;
            if (string.IsNullOrEmpty(connectionString)) return NotFound("Connection string is not set");

            ConnectParameters connector = Common.GetConnectParameters(connectionString, container, source);
            if (connector == null) return BadRequest();
            DbUtilities dbConn = new DbUtilities();
            dbConn.OpenConnection(connector);

            string result = "[]";
            try
            {
                fileStorageService.SetConnectionString(tmpConnString);
                string accessJson = await fileStorageService.ReadFile("connectdefinition", "PPDMDataAccess.json");
                _accessDefs = JsonConvert.DeserializeObject<List<DataAccessDef>>(accessJson);
                RuleModel rule = GetRule(dbConn, id);
                result = GetFailedObjects(dbConn, rule.RuleKey);
            }
            catch (Exception)
            {
                return BadRequest();
            }
            

            dbConn.CloseConnection();
            return result;
        }

        [HttpPost]
        public async Task<ActionResult<string>> ExecuteRule(DataQCParameters qcParams)
        {
            try
            {
                if (qcParams == null) return BadRequest();
                _accessDefs = JsonConvert.DeserializeObject<List<DataAccessDef>>(qcParams.DataAccessDefinitions);

                ConnectParameters connector = Common.GetConnectParameters(connectionString, container, qcParams.DataConnector);
                if (connector == null) return BadRequest();
                DbUtilities dbConn = new DbUtilities();
                dbConn.OpenConnection(connector);

                RuleModel rule = GetRule(dbConn, qcParams.RuleId);

                manageQCFlags = new ManageIndexTable(_accessDefs, connector.ConnectionString, rule.DataType);

                if (qcParams.ClearQCFlags)
                {
                    manageQCFlags.ClearQCFlags(qcParams.ClearQCFlags);
                }

                manageQCFlags.InitQCFlags(qcParams.ClearQCFlags);
                QualityCheckDataType(dbConn, rule, connector);
                dbConn.CloseConnection();
                manageQCFlags.SaveQCFlags();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.ToString());
            }

            return Ok($"OK");
        }

        private void QualityCheckDataType(DbUtilities dbConn, RuleModel rule, ConnectParameters connector)
        {
            QcRuleSetup qcSetup = new QcRuleSetup();
            qcSetup.Database = connector.Database;
            qcSetup.DatabasePassword = connector.DatabasePassword;
            qcSetup.DatabaseServer = connector.DatabaseServer;
            qcSetup.DatabaseUser = connector.DatabaseUser;
            DataAccessDef ruleAccessDef = _accessDefs.First(x => x.DataType == "Index");
            string ruleFilter = rule.RuleFilter;
            string jsonRules = JsonConvert.SerializeObject(rule);
            qcSetup.RuleObject = jsonRules;
            bool externalQcMethod = rule.RuleFunction.StartsWith("http");
            //QCMethods internalQC = new QCMethods();

            if (rule.RuleFunction == "Uniqueness") CalculateKey(rule);

            DataTable indexTable = manageQCFlags.GetIndexTable();
            foreach (DataRow idxRow in indexTable.Rows)
            {
                string jsonData = idxRow["JSONDATAOBJECT"].ToString();
                if (!string.IsNullOrEmpty(jsonData))
                {
                    qcSetup.IndexId = Convert.ToInt32(idxRow["INDEXID"]);
                    qcSetup.IndexNode = idxRow["Text_IndexNode"].ToString();
                    string qcStr = manageQCFlags.GetQCFlag(qcSetup.IndexId);
                    qcSetup.DataObject = jsonData;
                    string result = "Passed";
                    if (!Filter(jsonData, ruleFilter)) 
                    {
                        if (externalQcMethod) 
                        {
                            result = ProcessQcRule(qcSetup, rule);
                        }
                        else
                        {
                            //result = internalQC.ProcessMethod(qcSetup, indexTable, dbConn);
                            Type type = typeof(QCMethods);
                            MethodInfo info = type.GetMethod(rule.RuleFunction);

                            result = (string)info.Invoke(null, new object[] { qcSetup, dbConn, indexTable });
                        }
                        if (result == "Failed")
                        {
                            qcStr = qcStr + rule.RuleKey + ";";
                            manageQCFlags.SetQCFlag(qcSetup.IndexId, qcStr);
                        }
                    } 
                }
            }
        }

        private void CalculateKey(RuleModel rule)
        {
            string[] keyAttributes = rule.RuleParameters.Split(';');
            if (keyAttributes.Length > 0)
            {
                DataTable indexTable = manageQCFlags.GetIndexTable();
                foreach (DataRow idxRow in indexTable.Rows)
                {
                    string keyText = "";
                    string jsonData = idxRow["JSONDATAOBJECT"].ToString();
                    if (!string.IsNullOrEmpty(jsonData))
                    {
                        JObject dataObject = JObject.Parse(jsonData);
                        foreach (string attribute in keyAttributes)
                        {
                            JToken value = dataObject.GetValue(attribute);
                            keyText = keyText + value;
                        }
                        if (!string.IsNullOrEmpty(keyText))
                        {
                            string key = keyText.GetSHA256Hash();
                            idxRow["DATAKEY"] = key;
                        }
                    }
                }
            }
        }

        private string ProcessQcRule(QcRuleSetup qcSetup, RuleModel rule)
        {
            string returnResult = "Passed";
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
                            returnResult = azureResponse.ToString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Exception error = new Exception("DataQcWithProgressBar: Problems with URL: ", ex);
                throw error;
            }
            return returnResult;
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
            _indexAccessDef = _accessDefs.First(x => x.DataType == "Index");
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

        private string GetFailedObjects(DbUtilities dbConn, string ruleKey)
        {
            List<DmsIndex> qcIndex = new List<DmsIndex>();
            DataAccessDef ruleAccessDef = _accessDefs.First(x => x.DataType == "Index");
            string sql = ruleAccessDef.Select;
            string query = $" where QC_STRING like '%{ruleKey};%'";
            DataTable idx = dbConn.GetDataTable(sql, query);
            foreach (DataRow idxRow in idx.Rows)
            {
                string dataType = idxRow["DATATYPE"].ToString();
                string indexId = idxRow["INDEXID"].ToString();
                string jsonData = idxRow["JSONDATAOBJECT"].ToString();
                int intIndexId = Convert.ToInt32(indexId);
                qcIndex.Add(new DmsIndex()
                {
                    Id = intIndexId,
                    DataType = dataType,
                    JsonData = jsonData
                });
            }
            string result = JsonConvert.SerializeObject(qcIndex);

            return result;
        }
    }
}
