using AutoMapper;
using DatabaseManager.Common.Data;
using DatabaseManager.Common.DBAccess;
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
        private readonly string _azureConnectionString;
        private string databaseConnectionString;
        private readonly string container = "sources";
        private List<DataAccessDef> _accessDefs;
        DataAccessDef _indexAccessDef;
        private DbUtilities _dbConn;
        private IMapper _mapper;
        private bool syncPredictions;
        private ManageIndexTable manageIndexTable;
        private DataTable indexTable;
        private static HttpClient Client = new HttpClient();
        private readonly DapperDataAccess _dp;
        private readonly IIndexDBAccess _indexData;

        public Predictions(string azureConnectionString)
        {
            _azureConnectionString = azureConnectionString;
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
            _dp = new DapperDataAccess();
            _indexData = new IndexDBAccess(_dp);
        }

        public async Task<List<PredictionCorrection>> GetPredictions(string source)
        {
            List<PredictionCorrection> predictionResults = new List<PredictionCorrection>();
            ConnectParameters connector = await GetConnector(source);
            RuleManagement rules = new RuleManagement(_azureConnectionString);
            string jsonString = await rules.GetActivePredictionRules(source);
            predictionResults = JsonConvert.DeserializeObject<List<PredictionCorrection>>(jsonString);

            IndexAccess idxAccess = new IndexAccess();
            foreach (PredictionCorrection predItem in predictionResults)
            {
                string query = $" where QC_STRING like '%{predItem.RuleKey};%'";
                predItem.NumberOfCorrections = idxAccess.IndexCountByQuery(query, connector.ConnectionString);
            }
            return predictionResults;
        }

        public async Task<List<DmsIndex>> GetPrediction(string source, int id)
        {
            List<DmsIndex> predictionResults = new List<DmsIndex>();
            ConnectParameters connector = await GetConnector(source);
            RuleManagement rules = new RuleManagement(_azureConnectionString);
            string jsonRule = await rules.GetRule(source, id);
            RuleModel rule = JsonConvert.DeserializeObject<RuleModel>(jsonRule);
            string query = $" where QC_STRING like '%{rule.RuleKey};%'";
            IndexAccess idxAccess = new IndexAccess();
            List<IndexModel> idxResults = idxAccess.SelectIndexesByQuery(query, connector.ConnectionString);
            foreach (var idxRow in idxResults)
            {
                predictionResults.Add(new DmsIndex()
                {
                    Id = idxRow.IndexId,
                    DataType = idxRow.DataType,
                    JsonData = idxRow.JsonDataObject
                });
            }
            return predictionResults;
        }

        public async Task ExecutePrediction(PredictionParameters parms)
        {
            string accessJson = await _fileStorage.ReadFile("connectdefinition", "PPDMDataAccess.json");
            _accessDefs = JsonConvert.DeserializeObject<List<DataAccessDef>>(accessJson);
            ConnectParameters connector = await GetConnector(parms.DataConnector);
            databaseConnectionString = connector.ConnectionString;

            _dbConn.OpenConnection(connector);

            string sourceConnector = GetSource();
            if (parms.DataConnector == sourceConnector) syncPredictions = true;
            else syncPredictions = false;

            RuleManagement rules = new RuleManagement(_azureConnectionString);
            string jsonRule = await rules.GetRule(parms.DataConnector, parms.PredictionId);
            RuleModel rule = JsonConvert.DeserializeObject<RuleModel>(jsonRule);
            string jsonFunction = await rules.GetFunctionByName(parms.DataConnector, rule.RuleFunction);
            RuleFunctions function = JsonConvert.DeserializeObject<RuleFunctions>(jsonFunction);
            string functionKey = "";
            if (!string.IsNullOrEmpty(function.FunctionKey)) functionKey = "?code=" + function.FunctionKey;
            rule.RuleFunction = function.FunctionUrl + functionKey;

            manageIndexTable = new ManageIndexTable(_accessDefs, connector.ConnectionString, rule.DataType, rule.FailRule);
            manageIndexTable.InitQCFlags(false);
            await MakePredictions(rule, connector);
            _dbConn.CloseConnection();
            manageIndexTable.SaveQCFlags();
        }

        private async Task MakePredictions(RuleModel rule, ConnectParameters connector)
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
                    qcSetup.IndexNode = idxRow["TextIndexNode"].ToString();
                    qcSetup.DataObject = jsonData;
                    PredictionResult result;
                    if (externalQcMethod)
                    {
                        result = ProcessPrediction(qcSetup, predictionURL, rule);
                        if (result.Status == "Server error") break;
                    }
                    else
                    {
                        Type type = typeof(PredictionMethods);
                        MethodInfo info = type.GetMethod(rule.RuleFunction);
                        result = (PredictionResult)info.Invoke(null, new object[] { qcSetup, _dbConn });
                    }
                    await ProcessResult(result, rule);
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
                if (!response.IsSuccessStatusCode)
                {
                    result.Status = "Server error";
                    return result;
                }
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

        private async Task ProcessResult(PredictionResult result, RuleModel rule)
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
                await SavePrediction(result, qcStr);
            }
            else
            {
                //FailedPredictions++;
            }
        }

        private async Task SavePrediction(PredictionResult result, string qcStr)
        {
            if (result.SaveType == "Update")
            {
                UpdateAction(result, qcStr);
            }
            else if (result.SaveType == "Insert")
            {
                InsertAction(result);
            }
            else if (result.SaveType == "Delete")
            {
                await DeleteAction(result, qcStr);
            }
            else
            {
                //logger.LogWarning($"Save type {result.SaveType} is not supported");
            }
        }

        private void UpdateAction(PredictionResult result, string qcStr)
        {
            string idxQuery = $" where INDEXID = {result.IndexId}";
            IndexAccess idxAccess = new IndexAccess();
            List<IndexModel> idxResults = idxAccess.SelectIndexesByQuery(idxQuery, databaseConnectionString);
            if (idxResults.Count == 1)
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
                    string dataType = idxResults[0].DataType;
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

        private void InsertAction(PredictionResult result)
        {
            InsertMissingObjectToIndex(result);
            if (syncPredictions)
            {
                InsertMissingObjectToDatabase(result);
            }
        }

        private async Task DeleteAction(PredictionResult result, string qcStr)
        {
            IndexModel idxResults= await _indexData.GetIndexFromSP(result.IndexId, databaseConnectionString);
            if (idxResults != null)
            {
                DeleteChildren(result.IndexId, qcStr);
                DeleteParent(qcStr, idxResults);
            }
            else
            {
                //logger.LogWarning("Cannot find data key during update");
            }

        }

        private async Task DeleteChildren(int id, string qcStr)
        {
            IEnumerable<IndexModel> dmsIndex = await _indexData.GetDescendantsFromSP(id, databaseConnectionString);
            foreach (IndexModel index in dmsIndex)
            {
                index.JsonDataObject = "";
                index.QC_String = qcStr;
                await _indexData.UpdateIndex(index, databaseConnectionString);
            }
        }

        private void DeleteParent(string qcStr, IndexModel idxResults)
        {
            string condition = $"INDEXID={idxResults.IndexId}";
            var rows = indexTable.Select(condition);
            rows[0]["JSONDATAOBJECT"] = "";
            rows[0]["QC_STRING"] = qcStr;
            indexTable.AcceptChanges();

            if (syncPredictions)
            {
                string dataType = idxResults.DataType;
                string dataKey = idxResults.DataKey;
                DataAccessDef ruleAccessDef = _accessDefs.First(x => x.DataType == dataType);
                string select = ruleAccessDef.Select;
                string dataTable = GetTable(select);
                string dataQuery = "where " + dataKey;
                _dbConn.DBDelete(dataTable, dataQuery);
            }
        }

        private void InsertMissingObjectToIndex(PredictionResult result)
        {
            IndexFileData indexdata = GetIndexFileData(result.DataType);
            if (indexdata.DataName != null)
            {
                JObject dataObject = JObject.Parse(result.DataObject);
                string dataName = dataObject[indexdata.NameAttribute].ToString();
                string dataType = result.DataType;
                DataAccessDef dataAccessDef = _accessDefs.First(x => x.DataType == dataType);
                string dataKey = GetDataKey(dataObject, dataAccessDef.Keys);
                int parentId = result.IndexId;
                string jsonData = result.DataObject;
                double latitude = -99999.0;
                double longitude = -99999.0;
                int nodeId = GeIndextNode(dataType, parentId);
                if (nodeId > 0) _dbConn.InsertIndex(nodeId, dataName, dataType, dataKey, jsonData, latitude, longitude);
            }
        }

        private void InsertMissingObjectToDatabase(PredictionResult result)
        {
            string jsonDataObject = result.DataObject;
            JObject dataObject = JObject.Parse(jsonDataObject);
            dataObject["ROW_CHANGED_BY"] = Environment.UserName;
            dataObject["ROW_CREATED_BY"] = Environment.UserName;
            jsonDataObject = dataObject.ToString();
            jsonDataObject = Helpers.Common.SetJsonDataObjectDate(jsonDataObject, "ROW_CHANGED_DATE");
            jsonDataObject = Helpers.Common.SetJsonDataObjectDate(jsonDataObject, "ROW_CREATED_DATE");
            string dataType = result.DataType;
            try
            {
                _dbConn.InsertDataObject(jsonDataObject, dataType);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private int GeIndextNode(string dataType, int parentid)
        {
            int nodeid = 0;
            string nodeName = dataType + "s";
            string query = $" where INDEXID = {parentid}";
            IndexAccess idxAccess = new IndexAccess();
            List<IndexModel> idxResults = idxAccess.SelectIndexesByQuery(query, databaseConnectionString);
            if (idxResults.Count == 1)
            {
                string indexNode = idxResults[0].TextIndexNode;
                int indexLevel = idxResults[0].IndexLevel + 1;
                string strProcedure = $"EXEC spGetNumberOfDescendants '{indexNode}', {indexLevel}";
                query = "";
                DataTable idx = _dbConn.GetDataTable(strProcedure, query);
                if (idx.Rows.Count == 1)
                {
                    string condition = $"DATANAME={nodeName}";
                    var rows = indexTable.Select(condition);
                    if (rows.Length > 0)
                    {
                        nodeid = Convert.ToInt32(rows[0]["INDEXID"]);
                    }
                }
                if (nodeid == 0)
                {
                    nodeid = _dbConn.InsertIndex(parentid, nodeName, nodeName, "", "", 0.0, 0.0);
                }
            }
            return nodeid;
        }

        private List<IndexFileData> GetIndexArray(string taxonomy)
        {
            List<IndexFileData> idxData;
            JArray result = new JArray();
            JArray JsonIndexArray = JArray.Parse(taxonomy);
            idxData = new List<IndexFileData>();
            foreach (JToken level in JsonIndexArray)
            {
                idxData.Add(ProcessJTokens(level));
                idxData = ProcessIndexArray(JsonIndexArray, level, idxData);
            }

            return idxData;
        }

        private static IndexFileData ProcessJTokens(JToken token)
        {
            IndexFileData idxDataObject = new IndexFileData();
            idxDataObject.DataName = (string)token["DataName"];
            idxDataObject.NameAttribute = token["NameAttribute"]?.ToString();
            idxDataObject.LatitudeAttribute = token["LatitudeAttribute"]?.ToString();
            idxDataObject.LongitudeAttribute = token["LongitudeAttribute"]?.ToString();
            idxDataObject.ParentKey = token["ParentKey"]?.ToString();
            if (token["UseParentLocation"] != null) idxDataObject.UseParentLocation = (Boolean)token["UseParentLocation"];
            if (token["Arrays"] != null)
            {
                idxDataObject.Arrays = token["Arrays"];
            }
            return idxDataObject;
        }

        private List<IndexFileData> ProcessIndexArray(JArray JsonIndexArray, JToken parent, List<IndexFileData> idxData)
        {
            List<IndexFileData> result = idxData;
            if (parent["DataObjects"] != null)
            {
                foreach (JToken level in parent["DataObjects"])
                {
                    result.Add(ProcessJTokens(level));
                    result = ProcessIndexArray(JsonIndexArray, level, result);
                }
            }
            return result;
        }

        private string GetDataKey(JObject dataObject, string dbKeys)
        {
            string dataKey = "";
            string and = "";
            string[] keys = dbKeys.Split(',');
            foreach (string key in keys)
            {
                string attribute = key.Trim();
                string attributeValue = "'" + dataObject[attribute].ToString() + "'";
                dataKey = dataKey + and + key.Trim() + " = " + attributeValue;
                and = " AND ";
            }
            return dataKey;
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

        private IndexFileData GetIndexFileData(string dataType)
        {
            IndexFileData indexdata = new IndexFileData();
            IndexRootJson rootJson = GetIndexRootData();
            string taxonomy = rootJson.Taxonomy;
            List<IndexFileData> idxData = GetIndexArray(taxonomy);
            indexdata = idxData.FirstOrDefault(s => s.DataName == dataType);
            return indexdata;
        }

        private IndexRootJson GetIndexRootData()
        {
            IndexRootJson rootJson = new IndexRootJson();
            string idxQuery = $" where INDEXNODE = '/'";
            IndexAccess idxAccess = new IndexAccess();
            List<IndexModel> idxResults = idxAccess.SelectIndexesByQuery(idxQuery, databaseConnectionString);
            if (idxResults.Count > 0)
            {
                string jsonStringObject = idxResults[0].JsonDataObject;
                rootJson = JsonConvert.DeserializeObject<IndexRootJson>(jsonStringObject);
            }
            return rootJson;
        }

        private string GetSource()
        {
            string source = "";
            IndexRootJson rootJson = GetIndexRootData();
            string jsonData = rootJson.Source;
            ConnectParameters sourceConn = JsonConvert.DeserializeObject<ConnectParameters>(jsonData);
            source = sourceConn.SourceName;
            return source;
        }
    }
}
