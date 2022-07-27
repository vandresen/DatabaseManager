using AutoMapper;
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
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Common.Helpers
{
    public class ReportEditManagement
    {
        private readonly string azureConnectionString;
        private readonly DapperDataAccess _dp;
        private readonly IFileStorageServiceCommon _fileStorage;
        private readonly IIndexDBAccess _indexData;
        private readonly IRuleData _ruleData;
        private List<DataAccessDef> _accessDefs;
        private IndexDataCollection myIndex;

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
    }
}
