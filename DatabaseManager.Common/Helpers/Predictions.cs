using AutoMapper;
using DatabaseManager.Common.Entities;
using DatabaseManager.Common.Services;
using DatabaseManager.Shared;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Common.Helpers
{
    public class Predictions
    {
        private readonly IFileStorageServiceCommon _fileStorage;
        private readonly ITableStorageServiceCommon _tableStorage;
        private readonly string container = "sources";
        private List<DataAccessDef> _accessDefs;
        DataAccessDef _indexAccessDef;
        private DbUtilities _dbConn;
        private IMapper _mapper;
        private bool syncPredictions;
        private ManageIndexTable manageIndexTable;
        private DataTable indexTable;
        private static HttpClient Client = new HttpClient();

        public Predictions(string azureConnectionString)
        {
            var builder = new ConfigurationBuilder();
            IConfiguration configuration = builder.Build();
            _fileStorage = new AzureFileStorageServiceCommon(configuration);
            _fileStorage.SetConnectionString(azureConnectionString);
            _tableStorage = new AzureTableStorageServiceCommon(configuration);
            _tableStorage.SetConnectionString(azureConnectionString);

            _dbConn = new DbUtilities();

            var config = new MapperConfiguration(cfg => {
                cfg.CreateMap<SourceEntity, ConnectParameters>().ForMember(dest => dest.SourceName, opt => opt.MapFrom(src => src.RowKey));
            });
            _mapper = config.CreateMapper();
        }

        public async Task<List<PredictionCorrection>> GetPredictions(string source)
        {
            List<PredictionCorrection> predictionResults = new List<PredictionCorrection>();

            string accessJson = await _fileStorage.ReadFile("connectdefinition", "PPDMDataAccess.json");
            _accessDefs = JsonConvert.DeserializeObject<List<DataAccessDef>>(accessJson);

            _indexAccessDef = _accessDefs.First(x => x.DataType == "Index");
            DataAccessDef ruleAccessDef = _accessDefs.First(x => x.DataType == "Rules");
            string sql = ruleAccessDef.Select;
            string query = " where Active = 'Y' and RuleType = 'Predictions' order by PredictionOrder";

            ConnectParameters connector = await GetConnector(source);
            _dbConn.OpenConnection(connector);

            DataTable dt = _dbConn.GetDataTable(sql, query);
            string jsonString = JsonConvert.SerializeObject(dt);
            predictionResults = JsonConvert.DeserializeObject<List<PredictionCorrection>>(jsonString);

            foreach (PredictionCorrection predItem in predictionResults)
            {
                sql = _indexAccessDef.Select;
                query = $" where QC_STRING like '%{predItem.RuleKey};%'";
                DataTable ft = _dbConn.GetDataTable(sql, query);
                predItem.NumberOfCorrections = ft.Rows.Count;
            }

            _dbConn.CloseConnection();
            return predictionResults;
        }

        public async Task ExecutePrediction(PredictionParameters parms)
        {
            string accessJson = await _fileStorage.ReadFile("connectdefinition", "PPDMDataAccess.json");
            _accessDefs = JsonConvert.DeserializeObject<List<DataAccessDef>>(accessJson);
            ConnectParameters connector = await GetConnector(parms.DataConnector);

            _dbConn.OpenConnection(connector);

            string sourceConnector = GetSource(_dbConn);
            if (parms.DataConnector == sourceConnector) syncPredictions = true;
            else syncPredictions = false;

            RuleModel rule = Common.GetRule(_dbConn, parms.PredictionId, _accessDefs);

            manageIndexTable = new ManageIndexTable(_accessDefs, connector.ConnectionString, rule.DataType, rule.FailRule);
            manageIndexTable.InitQCFlags(false);
            MakePredictions(rule, connector);
            _dbConn.CloseConnection();
            manageIndexTable.SaveQCFlags();

        }

        private void MakePredictions(RuleModel rule, ConnectParameters connector)
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
                        result = ProcessPrediction(qcSetup, predictionURL, rule);
                    }
                    else
                    {
                        Type type = typeof(PredictionMethods);
                        MethodInfo info = type.GetMethod(rule.RuleFunction);
                        result = (PredictionResult)info.Invoke(null, new object[] { qcSetup, _dbConn });
                    }
                    ProcessResult(result, rule);
                }
            }
        }

        private PredictionResult ProcessPrediction(QcRuleSetup qcSetup, string predictionURL, RuleModel rule)
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
                //logger.LogWarning("ProcessDataObject: Problems with URL");
            }
            return result;
        }

        private void ProcessResult(PredictionResult result, RuleModel rule)
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
                SavePrediction(result, qcStr);
            }
            else
            {
                //FailedPredictions++;
            }
        }

        private void SavePrediction(PredictionResult result, string qcStr)
        {
            if (result.SaveType == "Update")
            {
                UpdateAction(result, qcStr);
            }
            else if (result.SaveType == "Insert")
            {
                //InsertAction(result, dbDAL);
            }
            else if (result.SaveType == "Delete")
            {
                DeleteAction(result, qcStr);
            }
            else
            {
                //logger.LogWarning($"Save type {result.SaveType} is not supported");
            }
        }

        private void UpdateAction(PredictionResult result, string qcStr)
        {
            DataAccessDef ruleAccessDef = _accessDefs.First(x => x.DataType == "Index");
            string select = ruleAccessDef.Select;
            string idxQuery = $" where INDEXID = {result.IndexId}";
            DataTable idx = _dbConn.GetDataTable(select, idxQuery);
            if (idx.Rows.Count == 1)
            {
                string condition = $"INDEXID={result.IndexId}";
                var rows = indexTable.Select(condition);
                rows[0]["JSONDATAOBJECT"] = result.DataObject;
                rows[0]["QC_STRING"] = qcStr;
                indexTable.AcceptChanges();

                if (syncPredictions)
                {
                    string jsonDataObject = result.DataObject;
                    JObject dataObject = JObject.Parse(jsonDataObject);
                    dataObject["ROW_CHANGED_BY"] = Environment.UserName;
                    jsonDataObject = dataObject.ToString();
                    jsonDataObject = Helpers.Common.SetJsonDataObjectDate(jsonDataObject, "ROW_CHANGED_DATE");
                    string dataType = idx.Rows[0]["DATATYPE"].ToString();
                    try
                    {
                        _dbConn.UpdateDataObject(jsonDataObject, dataType);
                    }
                    catch (Exception ex)
                    {
                        string error = ex.ToString();
                        //logger.LogWarning($"Error updating data object");
                        throw;
                    }

                }
            }
            else
            {
                //logger.LogWarning("Cannot find data key during update");
            }
        }

        private void DeleteAction(PredictionResult result, string qcStr)
        {
            DataAccessDef ruleAccessDef = _accessDefs.First(x => x.DataType == "Index");
            string select = ruleAccessDef.Select;
            string idxTable = GetTable(select);
            string idxQuery = $" where INDEXID = {result.IndexId}";
            DataTable idx = _dbConn.GetDataTable(select, idxQuery);
            if (idx.Rows.Count == 1)
            {
                string condition = $"INDEXID={result.IndexId}";
                var rows = indexTable.Select(condition);
                rows[0]["JSONDATAOBJECT"] = "";
                rows[0]["QC_STRING"] = qcStr;
                indexTable.AcceptChanges();

                if (syncPredictions)
                {
                    string dataType = idx.Rows[0]["DATATYPE"].ToString();
                    string dataKey = idx.Rows[0]["DATAKEY"].ToString();
                    ruleAccessDef = _accessDefs.First(x => x.DataType == dataType);
                    select = ruleAccessDef.Select;
                    string dataTable = GetTable(select);
                    string dataQuery = "where " + dataKey;
                    _dbConn.DBDelete(dataTable, dataQuery);
                }
            }
            else
            {
                //logger.LogWarning("Cannot find data key during update");
            }

        }

        private string GetTable(string select)
        {
            select = select.ToUpper();
            int from = select.IndexOf(" FROM ") + 6;
            string table = select.Substring(from);
            return table;
        }

        private async Task<ConnectParameters> GetConnector(string connectorStr)
        {
            if (String.IsNullOrEmpty(connectorStr))
            {
                Exception error = new Exception($"DataQc: Connection string is not set");
                throw error;
            }
            ConnectParameters connector = new ConnectParameters();
            SourceEntity entity = await _tableStorage.GetTableRecord<SourceEntity>(container, connectorStr);
            if (entity == null)
            {
                Exception error = new Exception($"DataQc: Could not find source connector");
                throw error;
            }
            connector = _mapper.Map<ConnectParameters>(entity);

            return connector;
        }

        private string GetSource(DbUtilities dbConn)
        {
            string source = "";
            DataAccessDef ruleAccessDef = _accessDefs.First(x => x.DataType == "Index");
            string select = ruleAccessDef.Select;
            string idxQuery = $" where INDEXNODE = '/'";
            DataTable idx = dbConn.GetDataTable(select, idxQuery);
            if (idx.Rows.Count == 1)
            {
                string jsonData = idx.Rows[0]["JSONDATAOBJECT"].ToString();
                ConnectParameters sourceConn = JsonConvert.DeserializeObject<ConnectParameters>(jsonData);
                source = sourceConn.SourceName;
            }
            else
            {
                //logger.LogWarning("Cannot get the source in root  index");
            }

            return source;
        }
    }
}
