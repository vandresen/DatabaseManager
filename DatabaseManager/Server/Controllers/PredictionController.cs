using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using DatabaseManager.Server.Entities;
using DatabaseManager.Server.Helpers;
using DatabaseManager.Server.Services;
using DatabaseManager.Shared;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore;
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
        private readonly IFileStorageService fileStorageService;
        private readonly ITableStorageService tableStorageService;
        private readonly IMapper mapper;
        private readonly ILogger<PredictionController> logger;
        private readonly IWebHostEnvironment _env;
        List<DataAccessDef> _accessDefs;
        DataAccessDef _indexAccessDef;
        private static HttpClient Client = new HttpClient();
        private ManageIndexTable manageIndexTable;
        private DataTable indexTable;

        public PredictionController(IConfiguration configuration,
            IFileStorageService fileStorageService,
            ITableStorageService tableStorageService,
            IMapper mapper,
            ILogger<PredictionController> logger,
            IWebHostEnvironment env)
        {
            connectionString = configuration.GetConnectionString("AzureStorageConnection");
            this.fileStorageService = fileStorageService;
            this.tableStorageService = tableStorageService;
            this.mapper = mapper;
            this.logger = logger;
            _env = env;
        }

        [HttpGet("{source}")]
        public async Task<ActionResult<List<PredictionCorrection>>> Get(string source)
        {
            DbUtilities dbConn = new DbUtilities();
            List<PredictionCorrection> predictionResuls = new List<PredictionCorrection>();
            try
            {
                string tmpConnString = Request.Headers["AzureStorageConnection"];
                tableStorageService.SetConnectionString(tmpConnString);
                SourceEntity entity = await tableStorageService.GetTableRecord<SourceEntity>(container, source);
                ConnectParameters connector = mapper.Map<ConnectParameters>(entity);
                fileStorageService.SetConnectionString(tmpConnString);
                string accessJson = await fileStorageService.ReadFile("connectdefinition", "PPDMDataAccess.json");
                _accessDefs = JsonConvert.DeserializeObject<List<DataAccessDef>>(accessJson);
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

        [HttpGet("{source}/{id}")]
        public async Task<ActionResult<string>> GetPredictions(string source, int id)
        {
            DbUtilities dbConn = new DbUtilities();
            string result = "[]";
            try
            {
                string tmpConnString = Request.Headers["AzureStorageConnection"];
                tableStorageService.SetConnectionString(tmpConnString);
                SourceEntity entity = await tableStorageService.GetTableRecord<SourceEntity>(container, source);
                ConnectParameters connector = mapper.Map<ConnectParameters>(entity);
                dbConn.OpenConnection(connector);
                fileStorageService.SetConnectionString(tmpConnString);
                string accessJson = await fileStorageService.ReadFile("connectdefinition", "PPDMDataAccess.json");
                _accessDefs = JsonConvert.DeserializeObject<List<DataAccessDef>>(accessJson);
                RuleModel rule = RuleUtilities.GetRule(dbConn, id, _accessDefs);
                result = GetPredictedObjects(dbConn, rule.RuleKey);
            }
            catch (Exception)
            {
                return BadRequest();
            }

            dbConn.CloseConnection();
            return result;
        }

        [HttpPost]
        public async Task<ActionResult<string>> ExecutePrediction(PredictionParameters predictionParams)
        {
            try
            {
                if (predictionParams == null) return BadRequest();
                _accessDefs = JsonConvert.DeserializeObject<List<DataAccessDef>>(predictionParams.DataAccessDefinitions);

                string tmpConnString = Request.Headers["AzureStorageConnection"];
                fileStorageService.SetConnectionString(tmpConnString);
                tableStorageService.SetConnectionString(tmpConnString);
                SourceEntity entity = await tableStorageService.GetTableRecord<SourceEntity>(container, predictionParams.DataConnector);
                ConnectParameters connector = mapper.Map<ConnectParameters>(entity);
                DbUtilities dbConn = new DbUtilities();
                dbConn.OpenConnection(connector);

                RuleModel rule = Common.GetRule(dbConn, predictionParams.PredictionId, _accessDefs);

                manageIndexTable = new ManageIndexTable(_accessDefs, connector.ConnectionString, rule.DataType, rule.FailRule);
                manageIndexTable.InitQCFlags(false);
                MakePredictions(rule, connector, dbConn);
                dbConn.CloseConnection();
                manageIndexTable.SaveQCFlags();
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
            qcSetup.Database = connector.Catalog;
            qcSetup.DatabasePassword = connector.Password;
            qcSetup.DatabaseServer = connector.DatabaseServer;
            qcSetup.DatabaseUser = connector.User;
            string jsonRules = JsonConvert.SerializeObject(rule);
            qcSetup.RuleObject = jsonRules;
            string predictionURL = rule.RuleFunction;

            bool externalQcMethod = rule.RuleFunction.StartsWith("http");

            indexTable = manageIndexTable.GetIndexTable();
            foreach (DataRow idxRow in indexTable.Rows)
            {
                string jsonData = idxRow["JSONDATAOBJECT"].ToString();
                if (!string.IsNullOrEmpty(jsonData))
                {
                    qcSetup.IndexId = Convert.ToInt32(idxRow["INDEXID"]);
                    qcSetup.IndexNode = idxRow["Text_IndexNode"].ToString();
                    qcSetup.DataObject = jsonData;
                    PredictionResult result;
                    if (externalQcMethod)
                    {
                        result = ProcessPrediction(qcSetup, predictionURL, rule, dbConn);
                    }
                    else
                    {
                        Type type = typeof(PredictionMethods);
                        MethodInfo info = type.GetMethod(rule.RuleFunction);
                        result = (PredictionResult)info.Invoke(null, new object[] { qcSetup, dbConn });
                    }
                    ProcessResult(result, rule, dbConn);
                }
            }
        }

        private PredictionResult ProcessPrediction(QcRuleSetup qcSetup, string predictionURL, RuleModel rule, DbUtilities dbConn)
        {
            PredictionResult result = new PredictionResult();
            try
            {
                var jsonString = JsonConvert.SerializeObject(qcSetup);
                var content = new StringContent(jsonString, Encoding.UTF8, "application/json");
                HttpResponseMessage response = Client.PostAsync(predictionURL, content).Result;
                using (HttpContent respContent = response.Content)
                {
                    string tr = respContent.ReadAsStringAsync().Result;
                    result = JsonConvert.DeserializeObject<PredictionResult>(tr);
                }
            }
            catch (Exception Ex)
            {
                logger.LogWarning("ProcessDataObject: Problems with URL");
            }
            return result;
        }

        private void ProcessResult(PredictionResult result, RuleModel rule, DbUtilities dbConn)
        {
            if (result.Status == "Passed")
            {
                string qcStr = manageIndexTable.GetQCFlag(result.IndexId);
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
                manageIndexTable.SetQCFlag(result.IndexId, qcStr);
                SavePrediction(result, dbConn, qcStr);
            }
            else
            {
                //FailedPredictions++;
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
                logger.LogWarning($"Save type {result.SaveType} is not supported");
            }
        }

        private void UpdateAction(PredictionResult result, DbUtilities dbConn, string qcStr)
        {
            DataAccessDef ruleAccessDef = _accessDefs.First(x => x.DataType == "Index");
            string select = ruleAccessDef.Select;
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
                logger.LogWarning("Cannot find data key during update");
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
            }
            else
            {
                logger.LogWarning("Cannot find data key during update");
            }

        }

        private List<PredictionCorrection> GetPredictionCorrections(DbUtilities dbConn)
        {
            List<PredictionCorrection> predictionResult = new List<PredictionCorrection>();
            _indexAccessDef = _accessDefs.First(x => x.DataType == "Index");
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

        private string GetTable(string select)
        {
            select = select.ToUpper();
            int from = select.IndexOf(" FROM ") + 6;
            string table = select.Substring(from);
            return table;
        }

        private string GetPredictedObjects(DbUtilities dbConn, string ruleKey)
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
