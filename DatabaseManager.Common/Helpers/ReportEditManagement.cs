﻿using AutoMapper;
using Azure;
using DatabaseManager.Common.Data;
using DatabaseManager.Common.DBAccess;
using DatabaseManager.Common.Entities;
using DatabaseManager.Common.Extensions;
using DatabaseManager.Common.Services;
using DatabaseManager.Shared;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using static DatabaseManager.Common.Helpers.RuleMethodUtilities;

namespace DatabaseManager.Common.Helpers
{
    public class ReportEditManagement
    {
        private readonly string azureConnectionString;
        private readonly DapperDataAccess _dp;
        private readonly IFileStorageServiceCommon _fileStorage;
        private readonly IIndexDBAccess _indexData;
        private readonly IRuleData _ruleData;
        private string _dbConnectionString;
        private List<DataAccessDef> _accessDefs;
        private DataAccessDef _accessDef;
        private IndexDataCollection myIndex;
        private List<ReferenceTable> _references;

        public ReportEditManagement()
        {
            _dp = new DapperDataAccess();
            _indexData = new IndexDBAccess(_dp);
        }

        public ReportEditManagement(string azureConnectionString)
        {
            _dp = new DapperDataAccess();
            this.azureConnectionString = azureConnectionString;
            var builder = new ConfigurationBuilder();
            IConfiguration configuration = builder.Build();
            _fileStorage = new AzureFileStorageServiceCommon(configuration);
            _fileStorage.SetConnectionString(azureConnectionString);
            _dp = new DapperDataAccess();
            _indexData = new IndexDBAccess(_dp);
            _ruleData = new RuleData(_dp);
        }

        public async Task<string> GetAttributeInfo(string sourceName, string dataType)
        {
            string json = "";
            ConnectParameters connector = await Common.GetConnectParameters(azureConnectionString, sourceName);

            string accessJson = await _fileStorage.ReadFile("connectdefinition", "PPDMDataAccess.json");
            List<DataAccessDef> accessDefs = JsonConvert.DeserializeObject<List<DataAccessDef>>(accessJson);
            DataAccessDef dataAccess = accessDefs.First(x => x.DataType == dataType);
            string sql = dataAccess.Select;
            string table = Common.GetTable(sql);
            string query = $" where 0 = 1";

            DbUtilities dbConn = new DbUtilities();
            dbConn.OpenWithConnectionString(connector.ConnectionString);
            DataTable dt = dbConn.GetDataTable(sql, query);
            AttributeInfo attributeInfo = new AttributeInfo();
            attributeInfo.DataAttributes = dt.GetColumnTypes();
            json  = JsonConvert.SerializeObject(attributeInfo);

            return json;
        }

        public async Task InsertEdits(ReportData reportData, string sourceName)
        {
            ConnectParameters connector = await Common.GetConnectParameters(azureConnectionString, sourceName);
            IndexModel index = await _indexData.GetIndexFromSP(reportData.Id, connector.ConnectionString);
            JObject dataObject = JObject.Parse(reportData.JsonData);

            if (reportData.ColumnType == "number")
            {
                dataObject[reportData.ColumnName] = reportData.NumberValue;
            }
            else if (reportData.ColumnType == "text")
            {
                dataObject[reportData.ColumnName] = reportData.TextValue;
            }
            else
            {
                Exception error = new Exception($"ReportEditManagement: Column type {reportData.ColumnType} not supported");
                throw error;
            }
            string remark = dataObject["REMARK"] + $";{reportData.ColumnName} has been manually edited;";
            dataObject["REMARK"] = remark;
            index.JsonDataObject = dataObject.ToString();
            string failRule = reportData.RuleKey + ";";
            index.QC_String = index.QC_String.Replace(failRule, "");
            await _indexData.UpdateIndex(index, connector.ConnectionString);
            if (connector.SourceType == "DataBase") UpdateDatabase(index.JsonDataObject, connector.ConnectionString, reportData.DataType);
        }

        public async Task DeleteEdits(string sourceName, int id)
        {
            ConnectParameters connector = await Common.GetConnectParameters(azureConnectionString, sourceName);
            IndexModel idxResults = await _indexData.GetIndexFromSP(id, connector.ConnectionString);
            if (idxResults != null)
            {
                await DeleteInIndex(id, idxResults.QC_String, connector.ConnectionString);
                if (connector.SourceType == "DataBase") DeleteInDatabase(connector, idxResults);
            }
            else
            {
                //logger.LogWarning("Cannot find data key during update");
            }
        }

        public async Task Merge(string sourceName, ReportData reportData)
        {
            ConnectParameters connector = await Common.GetConnectParameters(azureConnectionString, sourceName);
            string connectionString = connector.ConnectionString;
            string accessJson = await _fileStorage.ReadFile("connectdefinition", "PPDMDataAccess.json");
            _accessDefs = JsonConvert.DeserializeObject<List<DataAccessDef>>(accessJson);

            myIndex = new IndexDataCollection();

            IndexModel failedObject = await _indexData.GetIndexFromSP(reportData.Id, connectionString);
            IEnumerable<RuleModel> rules = await _ruleData.GetRulesFromSP(connectionString);
            RuleModel rule = rules.FirstOrDefault(x => x.RuleKey == reportData.RuleKey);
            IEnumerable<IndexModel> duplicates = await _indexData.GetIndexesWithQcStringFromSP(reportData.RuleKey, connector.ConnectionString);
            foreach (var duplicate in duplicates)
            {
                if (duplicate.IndexId != reportData.Id)
                {
                    string key = duplicate.JsonDataObject.GetUniqKey(rule);
                    if (key == reportData.TextValue)
                    {
                        await MergeIndexChildren(connectionString, duplicate.IndexId, 
                            reportData.Id, reportData.RuleKey);
                    }
                }
            }
        }

        public async Task InsertChild(string sourceName, ReportData reportData)
        {
            ConnectParameters connector = await Common.GetConnectParameters(azureConnectionString, sourceName);
            string connectionString = connector.ConnectionString;
            _dbConnectionString = connectionString;
            IEnumerable<RuleModel> rules = await _ruleData.GetRulesFromSP(connectionString);
            RuleModel rule = rules.FirstOrDefault(x => x.RuleKey == reportData.RuleKey);
            EntiretyParms parms = JsonConvert.DeserializeObject<EntiretyParms>(rule.RuleParameters);

            IndexModel parentObject = await _indexData.GetIndexFromSP(reportData.Id, connectionString);
            IndexRootJson rootJson = await GetIndexRoot(connectionString);
            _accessDef = rootJson.Source.GetDataAccessDefintionFromSourceJson(parms.DataType);

            string taxonomy = rootJson.Taxonomy;
            JArray JsonIndexArray = JArray.Parse(taxonomy);
            List<IndexFileData> idxData = new List<IndexFileData>();
            foreach (JToken level in JsonIndexArray)
            {
                idxData.Add(ProcessJTokens(level));
                idxData = ProcessIndexArray(JsonIndexArray, level, idxData);
            }
            IndexFileData taxonomyInfoForMissingObject = idxData.FirstOrDefault(x => x.DataName == parms.DataType);

            string dataTypeSql = _accessDef.Select;
            string table = Common.GetTable(dataTypeSql);
            string dataModel = await _fileStorage.ReadFile("ppdm39", "TAB.sql");
            IEnumerable<TableSchema> attributeProperties = Common.GetColumnInfo(table.ToLower(), dataModel);

            string referenceJson = await _fileStorage.ReadFile("connectdefinition", "PPDMReferenceTables.json");
            _references = JsonConvert.DeserializeObject<List<ReferenceTable>>(referenceJson);

            string emptyJson = Common.CreateJsonForNewDataObject(_accessDef, attributeProperties);
            string json = Common.UpdateJsonAttribute(emptyJson, taxonomyInfoForMissingObject.NameAttribute, parms.Name);
            json = PopulateJsonWithKeyValues(taxonomyInfoForMissingObject, json, parentObject);

            await InsertNewObjectToIndex(json, parms.DataType, taxonomyInfoForMissingObject, parentObject);
            if (connector.SourceType == "DataBase")
            {
                await InsertNewObjectToDatabase(json, parms.DataType);
            }
            string failRule = reportData.RuleKey + ";";
            parentObject.QC_String = parentObject.QC_String.Replace(failRule, "");
            await _indexData.UpdateIndex(parentObject, connector.ConnectionString);
        }

        private async Task MergeIndexChildren(string connectionString, int oldId, int newId, string ruleKey)
        {
            Dictionary<string, int> nodes = new Dictionary<string, int>();

            IEnumerable<IndexModel> newIndex = await _indexData.GetDescendantsFromSP(newId, connectionString);
            foreach (var index in newIndex)
            {
                if(string.IsNullOrEmpty(index.DataKey))
                {
                    nodes.Add(index.DataType, index.IndexId);
                }
            }

            IEnumerable<IndexModel> oldIndex = await _indexData.GetDescendantsFromSP(oldId, connectionString);
            IndexModel newParentIndex = newIndex.FirstOrDefault(x => x.IndexId == newId);
            IndexModel oldParentIndex = oldIndex.FirstOrDefault(x => x.IndexId == oldId);
            foreach (var index in oldIndex)
            {
                if (index.IndexId != oldId)
                {
                    string name = index.DataName;
                    IndexModel idx = newIndex.FirstOrDefault(x => x.DataName == name);
                    if (idx == null)
                    {
                        if (index.DataType == index.DataName)
                        {
                            if (!nodes.ContainsKey(index.DataName))
                            {
                                DbUtilities dbConn = new DbUtilities();
                                dbConn.OpenWithConnectionString(connectionString);
                                int nodeId = dbConn.InsertIndex(newId, index.DataName, index.DataType, "", "", 0.0, 0.0);
                                dbConn.CloseConnection();
                                nodes.Add(index.DataType, nodeId);
                            }
                        }
                        else
                        {
                            string newJson = GetMergeJson(newParentIndex.DataKey, index.JsonDataObject);
                            string newDataKey = GetMergeKey(newParentIndex.DataKey, oldParentIndex.DataKey, index.DataKey);
                            DbUtilities dbConn = new DbUtilities();
                            dbConn.OpenWithConnectionString(connectionString);
                            int parentId = nodes[(index.DataType + "s")];
                            double lat = -99999.0;
                            double lon = -99999.0;
                            if (index.Latitude != null) lat = (double)index.Latitude;
                            if (index.Longitude != null) lon = (double)index.Longitude;
                            int nodeId = dbConn.InsertIndex(parentId, index.DataName, index.DataType, newDataKey, 
                                newJson, lat, lon);

                            DataAccessDef dataAccessDef = _accessDefs.First(x => x.DataType == index.DataType);
                            string table = Common.GetTable(dataAccessDef.Select);
                            string sql = $"Update {table} Set {newParentIndex.DataKey} Where {index.DataKey}";
                            dbConn.SQLExecute(sql);
                            dbConn.CloseConnection();

                            index.JsonDataObject = "";
                            index.QC_String = "";
                            await _indexData.UpdateIndex(index, connectionString);
                        }
                    }
                }
            }

            oldParentIndex.JsonDataObject = "";
            oldParentIndex.QC_String = "";
            await _indexData.UpdateIndex(oldParentIndex, connectionString);

            DbUtilities cn = new DbUtilities();
            cn.OpenWithConnectionString(connectionString);
            string dataType = oldParentIndex.DataType;
            string dataKey = oldParentIndex.DataKey;
            DataAccessDef accessDef = _accessDefs.First(x => x.DataType == dataType);
            string select = accessDef.Select;
            string dataTable = Common.GetTable(select);
            string dataQuery = "where " + dataKey;
            cn.DBDelete(dataTable, dataQuery);
            cn.CloseConnection();

            newParentIndex.QC_String = newParentIndex.QC_String.Replace((ruleKey+";"), "");
            await _indexData.UpdateIndex(newParentIndex, connectionString);
        }

        private string GetMergeJson(string dataKey, string json)
        {
            string newJson = json;
            string[] keyAttributes = dataKey.Split(new string[] { "AND" }, StringSplitOptions.None);
            foreach (var keyAttribute in keyAttributes)
            {
                string key = keyAttribute.Trim();
                string[] keyPair = dataKey.Split('=');
                newJson = newJson.ModifyJson(keyPair[0].Trim(), keyPair[1].Trim());
            }
            return newJson;
        }

        private string GetMergeKey(string newParentDataKey, string oldParentDataKey, string oldIndexDataKey)
        {
            string key = oldIndexDataKey.Replace(oldParentDataKey, newParentDataKey);
            return key;
        }

        private async Task DeleteInIndex(int id, string qcString, string connectionString)
        {
            IEnumerable<IndexModel> dmsIndex = await _indexData.GetDescendantsFromSP(id, connectionString);
            foreach (IndexModel index in dmsIndex)
            {
                index.JsonDataObject = "";
                index.QC_String = "";
                await _indexData.UpdateIndex(index, connectionString);
            }
        }

        private void DeleteInDatabase(ConnectParameters connector, IndexModel indexItem)
        {
            string dataType = indexItem.DataType;
            string dataKey = indexItem.DataKey;
            List<DataAccessDef> accessDefs = JsonConvert.DeserializeObject<List<DataAccessDef>>(connector.DataAccessDefinition);
            DataAccessDef accessDef = accessDefs.First(x => x.DataType == dataType);
            string select = accessDef.Select;
            string dataTable = Common.GetTable(select);
            string dataQuery = "where " + dataKey;
            DbUtilities dbConn = new DbUtilities();
            dbConn.OpenWithConnectionString(connector.ConnectionString);
            dbConn.DBDelete(dataTable, dataQuery);
            dbConn.CloseConnection();
        }

        private void UpdateDatabase(string jsonDataObject, string connectionString, string dataType)
        {
            JObject dataObject = JObject.Parse(jsonDataObject);
            dataObject["ROW_CHANGED_BY"] = Environment.UserName;
            jsonDataObject = dataObject.ToString();
            jsonDataObject = Helpers.Common.SetJsonDataObjectDate(jsonDataObject, "ROW_CHANGED_DATE");
            DbUtilities dbConn = new DbUtilities();
            dbConn.OpenWithConnectionString(connectionString);
            dbConn.UpdateDataObject(jsonDataObject, dataType);
            dbConn.CloseConnection();
        }

        public async Task<IndexRootJson> GetIndexRoot(string dataConnector)
        {
            IndexModel idxResult = await _indexData.GetIndexRoot(dataConnector);
            string jsonStringObject = idxResult.JsonDataObject;
            IndexRootJson rootJson = JsonConvert.DeserializeObject<IndexRootJson>(jsonStringObject);
            return rootJson;
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

        private string PopulateJsonWithKeyValues(IndexFileData taxonomyInfo, string emptyJson, IndexModel parentObject)
        {
            string json = emptyJson;
            string[] parentKeys = taxonomyInfo.ParentKey.Split(',').Select(key => key.Trim()).ToArray();
            foreach (var parentKey in parentKeys)
            {
                int start = parentKey.IndexOf("[") + 1;
                int to = parentKey.IndexOf("]");
                int end = parentKey.IndexOf("=");
                if (start < 0 || to < 0)
                {
                    throw new Exception($"ReportEditManagement: Parent key definition in Taxonomy is not valid");
                }
                string attribute = parentKey.Substring(start, (to - start));
                attribute = attribute.Trim();
                string key = parentKey.Substring(0, end).Trim();
                JObject jsonObject = JObject.Parse(parentObject.JsonDataObject);
                string name = (string)jsonObject[attribute];
                json = Common.UpdateJsonAttribute(json, key, name);
            }

            if (string.IsNullOrEmpty(_accessDef.Constants) == false)
            {
                Dictionary<string, string> constants = _accessDef.Constants.ToStringDictionary();
                foreach (var kvp in constants)
                {
                    json = Common.UpdateJsonAttribute(json, kvp.Key, kvp.Value);
                }
            }

            JObject obj = JObject.Parse(json);
            string[] keys = _accessDef.Keys.Split(',').Select(key => key.Trim()).ToArray();
            foreach (string key in keys)
            {
                if (string.IsNullOrEmpty(obj[key].ToString()) == true)
                {
                    throw new Exception($"ReportEditManagement: Not all keys have a value");
                }
            }

            return json;
        }

        private async Task InsertNewObjectToDatabase(string jsonDataObject, string dataType)
        {
            JObject dataObject = JObject.Parse(jsonDataObject);
            dataObject["ROW_CHANGED_BY"] = Environment.UserName;
            dataObject["ROW_CREATED_BY"] = Environment.UserName;
            jsonDataObject = dataObject.ToString();
            jsonDataObject = Helpers.Common.SetJsonDataObjectDate(jsonDataObject, "ROW_CHANGED_DATE");
            jsonDataObject = Helpers.Common.SetJsonDataObjectDate(jsonDataObject, "ROW_CREATED_DATE");
            string storedProcedure = "dbo.spInsert" + dataType;
            try
            {
                await UpdateReferenceTables(dataType, dataObject);
                await _dp.SaveData(storedProcedure, new { json = jsonDataObject }, _dbConnectionString);
            }
            catch (Exception ex)
            {
                string message = ex.GetBaseException().Message;
                Exception error = new Exception($"ReportEditManagement: Could not insert {dataType} object, more info: \n {message}");
                throw error;
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
                await _dp.SaveDataSQL(insertQuery, new { value = value }, _dbConnectionString);
            }
        }

        private async Task InsertNewObjectToIndex(string json, string dataType, IndexFileData taxonomyInfoForMissingObject,
            IndexModel parentObject)
        {
            JObject dataObject = JObject.Parse(json);
            string dataName = dataObject[taxonomyInfoForMissingObject.NameAttribute].ToString();
            string dataKey = GetDataKey(dataObject, _accessDef.Keys);
            int parentId = parentObject.IndexId;
            double latitude = -99999.0;
            double longitude = -99999.0;
            if (taxonomyInfoForMissingObject.UseParentLocation)
            {
                if (parentObject.Latitude != null) latitude = (double)parentObject.Latitude;
                if (parentObject.Longitude != null) longitude = (double)parentObject.Longitude;
            }
            int nodeId = await GeIndextNode(dataType, parentObject);
            IndexModel indexModel = new IndexModel();
            indexModel.Latitude = latitude;
            indexModel.Longitude = longitude;
            indexModel.DataType = dataType;
            indexModel.DataName = dataName;
            indexModel.DataKey = dataKey;
            indexModel.JsonDataObject = json;
            if (nodeId > 0)
            {
                int result = await _indexData.InsertSingleIndex(indexModel, nodeId, _dbConnectionString);
            }
        }

        private async Task<int> GeIndextNode(string dataType, IndexModel idxResult)
        {
            int nodeid = 0;
            string nodeName = dataType + "s";
            if (idxResult != null)
            {
                IEnumerable<IndexModel> indexes = await _indexData.GetDescendantsFromSP(idxResult.IndexId, _dbConnectionString);
                if (indexes.Count() > 1)
                {
                    var nodeIndex = indexes.FirstOrDefault(x => x.DataType == nodeName);
                    if (nodeIndex != null) 
                    {
                        nodeid = nodeIndex.IndexId;
                    }
                }
                if (nodeid == 0)
                {
                    IndexModel nodeIndex = new IndexModel();
                    nodeIndex.Latitude = 0.0;
                    nodeIndex.Longitude = 0.0;
                    nodeIndex.DataType= nodeName;
                    nodeIndex.DataName= nodeName;
                    nodeid = await _indexData.InsertSingleIndex(nodeIndex, idxResult.IndexId, _dbConnectionString);
                }
            }
            return nodeid;
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

        private class EntiretyParms
        {
            public string DataType { get; set; }
            public string Name { get; set; }
        }
    }
}
