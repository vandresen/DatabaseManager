using DatabaseManager.Common.Data;
using DatabaseManager.Common.DBAccess;
using DatabaseManager.Common.Entities;
using DatabaseManager.Common.Extensions;
using DatabaseManager.Common.Services;
using DatabaseManager.Shared;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DatabaseManager.Common.Helpers
{
    public class Predictions
    {
        private readonly IFileStorageServiceCommon _fileStorage;
        private readonly string _azureConnectionString;
        private string databaseConnectionString;
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
            ConnectParameters connector = await GetConnector(parms.DataConnector);
            databaseConnectionString = connector.ConnectionString;

            _dbConn.OpenConnection(connector);

            string sourceConnector = await GetSource();
            syncPredictions = false;
            //if (parms.DataConnector == sourceConnector) syncPredictions = true;
            //else syncPredictions = false;

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

        public async Task SyncPredictions(PredictionParameters parms)
        {
            _log.LogInformation($"Setting up for syncing predictions");
            string referenceJson = await _fileStorage.ReadFile("connectdefinition", "PPDMReferenceTables.json");
            _references = JsonConvert.DeserializeObject<List<ReferenceTable>>(referenceJson);
            RuleManagement rules = new RuleManagement(_azureConnectionString);
            string activeRulesJson = await rules.GetActiveRules(parms.DataConnector);
            List<RuleModel> activeRules = JsonConvert.DeserializeObject<List<RuleModel>>(activeRulesJson);
            RuleModel rule = activeRules.FirstOrDefault(x => x.Id == parms.PredictionId);
            RuleModel qcRule = activeRules.FirstOrDefault(x => x.RuleKey == rule.FailRule);
            entiretyPrediction = false;
            if (qcRule != null) 
            {
                if (qcRule.RuleType == "Entirety") 
                { 
                    entiretyPrediction = true;
                    JObject ruleParObject = JObject.Parse(rule.RuleParameters);
                    string entiretyDataType = ruleParObject["DataType"].ToString();
                    entiretyDataName = qcRule.RuleParameters;
                }
            }

            ConnectParameters connector = await GetConnector(parms.DataConnector);
            IndexModel root = await _indexData.GetIndexRoot(connector.ConnectionString);
            IndexRootJson rootJson = JsonConvert.DeserializeObject<IndexRootJson>(root.JsonDataObject);
            ConnectParameters target = JsonConvert.DeserializeObject<ConnectParameters>(rootJson.Source);
            if (target.SourceType != "DataBase")
            {
                _log.LogError($"Target database {target.SourceName} is not a database");
                Exception error = new Exception($"Target database {target.SourceName} is not a database");
                throw error;
            }
            _accessDefs = JsonConvert.DeserializeObject<List<DataAccessDef>>(target.DataAccessDefinition);
            IEnumerable<IndexModel> indexes = await _indexData.GetIndexesWithQcStringFromSP(rule.RuleKey, connector.ConnectionString);
            if (indexes.Count() > 0) await SyncObject(indexes, target.ConnectionString);
        }

        private async Task SyncObject(IEnumerable<IndexModel> indexes, string conncetionString)
        {
            string jsonData = indexes.FirstOrDefault()?.JsonDataObject;
            if (string.IsNullOrEmpty(jsonData))
            {
                await SyncObjectDelete(indexes, conncetionString);
            }
            else if (entiretyPrediction)
            {
                await SyncObjectEntirety(indexes, conncetionString);
            }
            else
            {
                await SyncObjectUpdateInsert(indexes, conncetionString);
            }
        }

        private async Task SyncObjectEntirety(IEnumerable<IndexModel> indexes, string connectionString)
        {
            foreach (var index in indexes)
            {
                IEnumerable<IndexModel> children = await _indexData.GetDescendantsFromSP(index.IndexId,connectionString);
                IndexModel newChild = children.FirstOrDefault(x => x.DataName == entiretyDataName);
                string jsonDataObject = newChild.JsonDataObject;
                JObject dataObject = JObject.Parse(jsonDataObject);
                dataObject["ROW_CHANGED_BY"] = Environment.UserName;
                dataObject["ROW_CREATED_BY"] = Environment.UserName;
                jsonDataObject = dataObject.ToString();
                jsonDataObject = Helpers.Common.SetJsonDataObjectDate(jsonDataObject, "ROW_CHANGED_DATE");
                jsonDataObject = Helpers.Common.SetJsonDataObjectDate(jsonDataObject, "ROW_CREATED_DATE");
                string dataType = newChild.DataType;
                string storedProcedure = "dbo.spInsert" + dataType;
                try
                {
                    await _dp.SaveData(storedProcedure, new { json = jsonDataObject }, connectionString);
                }
                catch (Exception ex)
                {
                    _log.LogInformation($"Sync: Could not insert {dataType} object, more info: {ex}");
                    Exception error = new Exception($"Sync: Could not insert {dataType} object, more info: \n {ex}");
                    throw error;
                }
            }
        }

        private async Task SyncObjectUpdateInsert(IEnumerable<IndexModel> indexes, string connectionString)
        {
            string json = indexes.FirstOrDefault()?.JsonDataObject;
            string datatype = indexes.FirstOrDefault()?.DataType;
            JObject jsonObject = JObject.Parse(json);

            // Extract the JSON properties and their types
            Dictionary<string, Type> columnDefinitions = new Dictionary<string, Type>();

            foreach (var property in jsonObject.Properties())
            {
                JTokenType propertyType = property.Value.Type;

                // Map JSON types to .NET types (you may need to add more cases as needed)
                Type columnType;
                switch (propertyType)
                {
                    case JTokenType.String:
                        columnType = typeof(string);
                        break;
                    case JTokenType.Integer:
                        columnType = typeof(int);
                        break;
                    case JTokenType.Float:
                        columnType = typeof(double);
                        break;
                    case JTokenType.Boolean:
                        columnType = typeof(bool);
                        break;
                    default:
                        columnType = typeof(string); // Default to string if the type is not recognized
                        break;
                }

                columnDefinitions.Add(property.Name, columnType);
            }

            // Create the temporary table in the SQL Server database
            string tempTableName = "#TempTable";
            string createTableQuery = $"CREATE TABLE {tempTableName} (";

            foreach (var column in columnDefinitions)
            {
                createTableQuery += $"{column.Key} {Common.GetSqlDbTypeString(column.Value)}, ";
            }

            createTableQuery = createTableQuery.TrimEnd(',', ' ') + ")";

            // Convert the JSON to a DataTable
            DataTable dataTable = new DataTable();
            foreach (var column in columnDefinitions)
            {
                dataTable.Columns.Add(column.Key, column.Value);
            }

            // Deserialize JSON and insert data into the DataTable
            foreach (var index in indexes)
            {
                json = index.JsonDataObject;
                jsonObject = JObject.Parse(json);
                DataRow dataRow = dataTable.NewRow();
                foreach (var property in jsonObject.Properties())
                {
                    dataRow[property.Name] = property.Value.ToObject(columnDefinitions[property.Name]);
                }
                dataTable.Rows.Add(dataRow);
            }

            //string CreateReferenceSql = CreateSqlToLoadReferences(datatype, tempTableName, dataTable);
            LoadNewReferences(datatype, connectionString, dataTable);


            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                using (SqlCommand createTableCommand = new SqlCommand(createTableQuery, connection))
                {
                    createTableCommand.ExecuteNonQuery();
                }

                using (SqlBulkCopy bulkCopy = new SqlBulkCopy(connection))
                {
                    bulkCopy.DestinationTableName = tempTableName;
                    bulkCopy.WriteToServer(dataTable);
                }

                string mergeSql = CreateSqlToMerge(dataTable, tempTableName, datatype);
                using (SqlCommand mergeCommand = new SqlCommand(mergeSql, connection))
                {
                    mergeCommand.ExecuteNonQuery();
                }

                string dropTableQuery = $"DROP TABLE {tempTableName}";
                using (SqlCommand dropTableCommand = new SqlCommand(dropTableQuery, connection))
                {
                    dropTableCommand.ExecuteNonQuery();
                }
            }
        }

        private string CreateSqlToMerge(DataTable dt, string tempTable, string dataType)
        {
            string sql = "";
            DataAccessDef objectAccessDef = _accessDefs.First(x => x.DataType == dataType);
            string dataTypeSql = objectAccessDef.Select;
            string table = Common.GetTable(dataTypeSql);
            string[] columnNames = dt.Columns.Cast<DataColumn>()
                         .Select(x => x.ColumnName)
                         .ToArray();
            string updateSql = "";
            string comma = "";
            foreach (string colName in columnNames)
            {
                updateSql = updateSql + comma + colName + " = B." + colName;
                comma = ",";
            }
            string insertSql = "";
            string valueSql = "";
            comma = "";
            foreach (string colName in columnNames)
            {
                insertSql = insertSql + comma + colName;
                valueSql = valueSql + comma + " B." + colName;
                comma = ",";
            }

            string[] keys = objectAccessDef.Keys.Split(',').Select(k => k.Trim()).ToArray();
            string and = "";
            string joinSql = "";
            foreach (string key in keys)
            {
                joinSql = joinSql + and + "A." + key + " = B." + key;
                and = " AND ";
            }

            sql = $"MERGE INTO {table} A " +
                $" USING {tempTable} B " +
                " ON " + joinSql +
                " WHEN MATCHED THEN " +
                " UPDATE " +
                " SET " + updateSql +
                " WHEN NOT MATCHED THEN " +
                " INSERT(" + insertSql + ") " +
                " VALUES(" + valueSql + "); ";
            return sql;
        }

        private void LoadNewReferences(string dataType, string connectionString, DataTable dt)
        {
            string insertSql = "";
            string comma = "";
            List<ReferenceTable> dataTypeRefs = _references.Where(x => x.DataType == dataType).ToList();
            foreach (ReferenceTable refTable in dataTypeRefs)
            {
                string valueAttribute = refTable.ValueAttribute;
                var distinctValues = dt.AsEnumerable()
                    .Select(row => row.Field<string>(refTable.ReferenceAttribute))
                    .Distinct();
                foreach (var value in distinctValues)
                {
                    string yourCondition = $"{refTable.KeyAttribute} = @Value";
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        connection.Open();
                        string insertColumns;
                        string selectColumns;
                        //string insertQuery = $"INSERT INTO {refTable.Table} ";
                        if (valueAttribute == refTable.KeyAttribute)
                        {
                            insertColumns = $"{refTable.KeyAttribute}";
                            selectColumns = $"{value}";
                        }
                        else
                        {
                            insertColumns = $"{refTable.KeyAttribute}, {valueAttribute}";
                            selectColumns = $"'{value}', '{value}'";
                        }
                        if (!string.IsNullOrEmpty(refTable.FixedKey))
                        {
                            string[] fixedKey = refTable.FixedKey.Split('=');
                            insertColumns = insertColumns + ", " + fixedKey[0];
                            selectColumns = selectColumns + ", '" + fixedKey[1] + "'";
                        }
                        string insertQuery = $"INSERT INTO {refTable.Table} ({insertColumns}) " +
                                             $"SELECT {selectColumns} " +
                                             $"WHERE NOT EXISTS (SELECT 1 FROM {refTable.Table} WHERE {yourCondition})";
                        using (SqlCommand command = new SqlCommand(insertQuery, connection))
                        {
                            command.Parameters.AddWithValue("@Value", value);
                            int rowsAffected = command.ExecuteNonQuery();
                        }
                    }
                }
            }
        }

        private async Task SyncObjectDelete(IEnumerable<IndexModel> indexes, string conncetionString)
        {
            foreach (var index in indexes)
            {
                DataAccessDef objectAccessDef = _accessDefs.First(x => x.DataType == index.DataType);
                string select = objectAccessDef.Select;
                string dataTable = GetTable(select);
                string dataQuery = "where " + index.DataKey;
                string sql = "Delete from " + dataTable + " " + dataQuery;
                _db.ExecuteSQL(sql, conncetionString);
            }
            
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
                //logger.LogWarning($"Save type {result.SaveType} is not supported");
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

        private async Task InsertAction(PredictionResult result)
        {
            await InsertMissingObjectToIndex(result);
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
                DataAccessDef ruleAccessDef = _accessDefs.First(x => x.DataType == dataType);
                string select = ruleAccessDef.Select;
                string dataTable = GetTable(select);
                string dataQuery = "where " + dataKey;
                _dbConn.DBDelete(dataTable, dataQuery);
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
            //IndexAccess idxAccess = new IndexAccess();
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
