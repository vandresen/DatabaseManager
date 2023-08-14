using DatabaseManager.Common.Data;
using DatabaseManager.Common.DBAccess;
using DatabaseManager.Common.Entities;
using DatabaseManager.Common.Services;
using DatabaseManager.Shared;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
        private readonly string _azureConnectionString;
        private string databaseConnectionString;
        private string syncConnectionString;
        private readonly string container = "sources";
        private List<DataAccessDef> _accessDefs;
        DataAccessDef _indexAccessDef;
        private DbUtilities _dbConn;
        private IADODataAccess _db;
        private bool syncPredictions;
        private bool entiretyPrediction;
        private string entiretyDataName = "";
        private ManageIndexTable manageIndexTable;
        private DataTable indexTable;
        private static HttpClient Client = new HttpClient();
        private readonly DapperDataAccess _dp;
        private readonly IIndexDBAccess _indexData;
        private readonly ILogger _log;
        private List<ReferenceTable> _references;

        public Predictions(string azureConnectionString, ILogger log)
        {
            _azureConnectionString = azureConnectionString;
            var builder = new ConfigurationBuilder();
            IConfiguration configuration = builder.Build();
            _fileStorage = new AzureFileStorageServiceCommon(configuration);
            _fileStorage.SetConnectionString(azureConnectionString);
            _dbConn = new DbUtilities();
            _db = new ADODataAccess();
            
            _dp = new DapperDataAccess();
            _indexData = new IndexDBAccess(_dp);
            _log = log;
        }

        public async Task<List<PredictionCorrection>> GetPredictions(string source)
        {
            List<PredictionCorrection> predictionResults = new List<PredictionCorrection>();
            ConnectParameters connector = await GetConnector(source);
            RuleManagement rules = new RuleManagement(_azureConnectionString);
            string jsonString = await rules.GetActivePredictionRules(source);
            predictionResults = JsonConvert.DeserializeObject<List<PredictionCorrection>>(jsonString);
            foreach (PredictionCorrection predItem in predictionResults)
            {
                predItem.NumberOfCorrections = await _indexData.GetCount(connector.ConnectionString, predItem.RuleKey);
            }
            return predictionResults;
        }

        public async Task ExecutePrediction(PredictionParameters parms)
        {
            _log.LogInformation($"Setting up for predictions");
            string accessJson = await _fileStorage.ReadFile("connectdefinition", "PPDMDataAccess.json");
            _accessDefs = JsonConvert.DeserializeObject<List<DataAccessDef>>(accessJson);
            string referenceJson = await _fileStorage.ReadFile("connectdefinition", "PPDMReferenceTables.json");
            _references = JsonConvert.DeserializeObject<List<ReferenceTable>>(referenceJson);
            ConnectParameters connector = await GetConnector(parms.DataConnector);
            databaseConnectionString = connector.ConnectionString;

            _dbConn.OpenConnection(connector);

            string sourceConnectorName = await GetSource();
            ConnectParameters sourceConnector = await GetConnector(sourceConnectorName);
            syncConnectionString = sourceConnector.ConnectionString;
            if (sourceConnector.SourceType == "DataBase") syncPredictions = true;
            else syncPredictions = false;

            RuleManagement rules = new RuleManagement(_azureConnectionString);
            RuleModel rule = await rules.GetRuleAndFunction(parms.DataConnector, parms.PredictionId);

            manageIndexTable = new ManageIndexTable(_accessDefs, connector.ConnectionString, rule.DataType, rule.FailRule);
            manageIndexTable.InitQCFlags(false);
            _log.LogInformation($"Start making predictions");
            await MakePredictions(rule, connector);
            _dbConn.CloseConnection();
            _log.LogInformation($"Saving qc flags");
            manageIndexTable.SaveQCFlags();
        }

        private async Task MakePredictions(RuleModel rule, ConnectParameters connector)
        {
            QcRuleSetup qcSetup = new QcRuleSetup();
            qcSetup.Database = connector.Catalog;
            qcSetup.DatabasePassword = connector.Password;
            qcSetup.DatabaseServer = connector.DatabaseServer;
            qcSetup.DatabaseUser = connector.User;
            qcSetup.DataConnector = connector.ConnectionString;
            string jsonRules = JsonConvert.SerializeObject(rule);
            qcSetup.RuleObject = jsonRules;
            string predictionURL = rule.RuleFunction;

            bool externalQcMethod = rule.RuleFunction.StartsWith("http");

            indexTable = manageIndexTable.GetIndexTable();
            _log.LogInformation($"Number of prediction object are {indexTable.Rows.Count}");
            foreach (DataRow idxRow in indexTable.Rows)
            {
                string jsonData = idxRow["JSONDATAOBJECT"].ToString();
                if (!string.IsNullOrEmpty(jsonData))
                {
                    qcSetup.IndexId = Convert.ToInt32(idxRow["INDEXID"]);
                    //_log.LogInformation($"Processing index id {qcSetup.IndexId} ");
                    qcSetup.IndexNode = idxRow["TextIndexNode"].ToString();
                    qcSetup.DataObject = jsonData;
                    PredictionResult result = new PredictionResult();
                    if (externalQcMethod)
                    {
                        result = ProcessPrediction(qcSetup, predictionURL, rule);
                        if (result.Status == "Server error") break;
                    }
                    else
                    {
                        Type type = typeof(PredictionMethods);
                        MethodInfo info = type.GetMethod(rule.RuleFunction);
                        result = (PredictionResult)info.Invoke(null, new object[] { qcSetup, _dbConn, _indexData });
                    }
                    await ProcessResult(result, rule);
                }
            }
            //_log.LogInformation($"Finished process all rows");
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
                _log.LogWarning("ProcessDataObject: Problems with URL");
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
                await UpdateAction(result, qcStr);
            }
            else if (result.SaveType == "Insert")
            {
                await InsertAction(result);
            }
            else if (result.SaveType == "Delete")
            {
                await DeleteAction(result, qcStr);
            }
            else
            {
                _log.LogWarning($"Save type {result.SaveType} is not supported");
            }
        }

        private async Task UpdateAction(PredictionResult result, string qcStr)
        {
            string idxQuery = $" where INDEXID = {result.IndexId}";
            IndexModel idxResult = await _indexData.GetIndex(result.IndexId, databaseConnectionString);
            if (idxResult != null)
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
                    string dataType = idxResult.DataType;
                    try
                    {
                        await UpdateReferenceTables(dataType, dataObject);
                        string storedProcedure = "dbo.spUpdate" + dataType;
                        await _dp.SaveData(storedProcedure, new { json = jsonDataObject }, syncConnectionString);
                    }
                    catch (Exception ex)
                    {
                        string error = ex.ToString();
                        _log.LogError($"Error updating data object: {error}");
                        throw;
                    }
                }
            }
            else
            {
                //logger.LogWarning("Cannot find data key during update");
            }
        }

        private async Task UpdateReferenceTables(string dataType, JObject dataObject)
        {
            List<ReferenceTable> dataTypeRefs = _references.Where(x => x.DataType == dataType).ToList();
            foreach (ReferenceTable refTable in dataTypeRefs)
            {
                string valueAttribute = refTable.ValueAttribute;
                string value = dataObject[refTable.KeyAttribute].ToString();
                string insertColumns;
                string selectColumns;
                string condition;
                if (valueAttribute == refTable.KeyAttribute)
                {
                    insertColumns = $"{refTable.KeyAttribute}";
                    selectColumns = $"@value";
                    condition = $"{refTable.KeyAttribute} = @value";
                }
                else
                {
                    insertColumns = $"{refTable.KeyAttribute}, {valueAttribute}";
                    selectColumns = $"@value, @value";
                    condition = $"{refTable.KeyAttribute} = @value";
                }
                if (!string.IsNullOrEmpty(refTable.FixedKey))
                {
                    string[] fixedKey = refTable.FixedKey.Split('=');
                    insertColumns = insertColumns + ", " + fixedKey[0];
                    selectColumns = selectColumns + ", '" + fixedKey[1] + "'";
                    condition = condition + " AND " + fixedKey[0] + " = '" + fixedKey[1] + "'";
                }
                string insertQuery = $"INSERT INTO {refTable.Table} ({insertColumns}) " +
                                     $"SELECT {selectColumns} " +
                                     $"WHERE NOT EXISTS (SELECT 1 FROM {refTable.Table} WHERE {condition})";
                await _dp.SaveDataSQL(insertQuery, new { value = value }, syncConnectionString);
            }
        }

        private async Task InsertAction(PredictionResult result)
        {
            await InsertMissingObjectToIndex(result);
            if (syncPredictions)
            {
                await InsertMissingObjectToDatabase(result);
            }
        }

        private async Task DeleteAction(PredictionResult result, string qcStr)
        {
            IndexModel idxResults= await _indexData.GetIndexFromSP(result.IndexId, databaseConnectionString);
            if (idxResults != null)
            {
                await DeleteChildren(result.IndexId, qcStr);
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
                DataAccessDef objectAccessDef = _accessDefs.First(x => x.DataType == dataType);
                string select = objectAccessDef.Select;
                string dataTable = GetTable(select);
                string dataQuery = "where " + dataKey;
                string sql = "Delete from " + dataTable + " " + dataQuery;
                _db.ExecuteSQL(sql, syncConnectionString);
            }
        }

        private async Task InsertMissingObjectToIndex(PredictionResult result)
        {
            IndexFileData indexdata = await GetIndexFileData(result.DataType);
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
                int nodeId = await GeIndextNode(dataType, parentId);
                if (nodeId > 0) _dbConn.InsertIndex(nodeId, dataName, dataType, dataKey, jsonData, latitude, longitude);
            }
        }

        private async Task InsertMissingObjectToDatabase(PredictionResult result)
        {
            string jsonDataObject = result.DataObject;
            JObject dataObject = JObject.Parse(jsonDataObject);
            dataObject["ROW_CHANGED_BY"] = Environment.UserName;
            dataObject["ROW_CREATED_BY"] = Environment.UserName;
            jsonDataObject = dataObject.ToString();
            jsonDataObject = Helpers.Common.SetJsonDataObjectDate(jsonDataObject, "ROW_CHANGED_DATE");
            jsonDataObject = Helpers.Common.SetJsonDataObjectDate(jsonDataObject, "ROW_CREATED_DATE");
            string dataType = result.DataType;
            string storedProcedure = "dbo.spInsert" + dataType;
            try
            {
                await UpdateReferenceTables(dataType, dataObject);
                await _dp.SaveData(storedProcedure, new { json = jsonDataObject }, syncConnectionString);
            }
            catch (Exception ex)
            {
                string message = ex.GetBaseException().Message; 
                Exception error = new Exception($"Predictions: Could not insert {dataType} object, more info: \n {message}");
                throw error;
            }
        }

        private async Task<int> GeIndextNode(string dataType, int parentid)
        {
            int nodeid = 0;
            string nodeName = dataType + "s";
            string query = $" where INDEXID = {parentid}";
            IndexModel idxResult = await _indexData.GetIndex(parentid, databaseConnectionString);
            if (idxResult != null)
            {
                string indexNode = idxResult.TextIndexNode;
                int indexLevel = idxResult.IndexLevel + 1;
                IEnumerable<DmsIndex> dmsIndex = await _indexData.GetNumberOfDescendantsByIdAndLevel(indexNode,
                    indexLevel, databaseConnectionString);
                if (dmsIndex.Count() > 1)
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
            Sources so = new Sources(_azureConnectionString);
            ConnectParameters connector = await so.GetSourceParameters(connectorStr);
            return connector;
        }

        private async Task<IndexFileData> GetIndexFileData(string dataType)
        {
            IndexFileData indexdata = new IndexFileData();
            IndexRootJson rootJson = await GetIndexRootData();
            string taxonomy = rootJson.Taxonomy;
            List<IndexFileData> idxData = GetIndexArray(taxonomy);
            indexdata = idxData.FirstOrDefault(s => s.DataName == dataType);
            return indexdata;
        }

        private async Task<IndexRootJson> GetIndexRootData()
        {
            IndexRootJson rootJson = new IndexRootJson();
            IndexModel idxResult = await _indexData.GetIndexRoot(databaseConnectionString);
            string jsonStringObject = idxResult.JsonDataObject;
            rootJson = JsonConvert.DeserializeObject<IndexRootJson>(jsonStringObject);
            return rootJson;
        }

        private async Task<string> GetSource()
        {
            string source = "";
            IndexRootJson rootJson = await GetIndexRootData();
            string jsonData = rootJson.Source;
            ConnectParameters sourceConn = JsonConvert.DeserializeObject<ConnectParameters>(jsonData);
            source = sourceConn.SourceName;
            return source;
        }
    }
}
