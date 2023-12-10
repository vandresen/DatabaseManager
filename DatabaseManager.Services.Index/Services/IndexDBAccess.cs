using DatabaseManager.Services.Index.Extensions;
using DatabaseManager.Services.Index.Helpers;
using DatabaseManager.Services.Index.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Polly;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.Index.Services
{
    public class IndexDBAccess : IIndexDBAccess
    {
        public JArray JsonIndexArray { get; set; }
        private List<DataAccessDef> dataAccessDefs = new List<DataAccessDef>();
        private List<IndexFileData> _idxData;
        private string _taxonomy;
        private IndexFileData _currentItem;
        private IDataAccess _sourceAccess;
        private IndexFileData _parentItem;
        private Location _location;
        private readonly IDataAccess _ida;
        private IndexDataCollection myIndex;
        private readonly IDapperDataAccess _dp;
        private readonly IDataSourceService _ds;
        private readonly IFileStorageService _fs;
        private readonly ILogger _log;
        private string _connectionString;
        private string _table = "pdo_qc_index";
        private string getSql = "Select IndexId, IndexNode.ToString() AS TextIndexNode, " +
            "IndexLevel, DataName, DataType, DataKey, QC_String, UniqKey, JsonDataObject, " +
            "Latitude, Longitude " +
            "from pdo_qc_index";

        public IndexDBAccess(IDapperDataAccess dp, IDataSourceService ds, 
            IFileStorageService fs, ILogger<IndexDBAccess> log)
        {
            _dp = dp;
            _ds = ds;
            _fs = fs;
            _log = log;
            _ida = new DBDataAccess();
        }

        public async Task BuildIndex(BuildIndexParameters idxParms)
        {
            _log.LogInformation($"BuildIndex: Start building index for source {idxParms.SourceName}");
            ResponseDto dsResponse = await _ds.GetDataSourceByNameAsync<ResponseDto>(idxParms.SourceName);
            ConnectParametersDto source = JsonConvert.DeserializeObject<ConnectParametersDto>(Convert.ToString(dsResponse.Result));
            _log.LogInformation("BuildIndex: Got the source info");
            ConnectParametersDto target = new ConnectParametersDto();
            if (!String.IsNullOrEmpty(idxParms.TargetName))
            {
                dsResponse = await _ds.GetDataSourceByNameAsync<ResponseDto>(idxParms.TargetName);
                _log.LogInformation("BuildIndex: Got the target info");
                target = JsonConvert.DeserializeObject<ConnectParametersDto>(Convert.ToString(dsResponse.Result));
                _ida.WakeUpDatabase(target.ConnectionString);
            }

            _fs.SetConnectionString(idxParms.StorageAccount);
            target.DataAccessDefinition = await _fs.ReadFile("connectdefinition", "PPDMDataAccess.json");
            _log.LogInformation("BuildIndex: Got the connect definition");

            int parentNodes = await Initialize(target, source, idxParms);

            List<ParentIndexNodes> nodes = await IndexParent(parentNodes, idxParms.Filter);
            for (int j = 0; j < nodes.Count; j++)
            {
                ParentIndexNodes node = nodes[j];
                for (int i = 0; i < node.NodeCount; i++)
                {
                    await IndexChildren(j, i, node.ParentNodeId);
                }
            }
            _log.LogInformation("BuildIndex: Inserting indexes");
            await InsertIndexes(myIndex, 0, _connectionString);
        }

        public async Task CreateDatabaseIndex(string connectionString)
        {
            CreateIndexDataModel dm = new CreateIndexDataModel();
            await dm.CreateDMSModel(connectionString);
            await dm.CreateIndexStoredProcedures(connectionString, getSql);
        }

        public Task<IEnumerable<DmIndexDto>> GetDmIndex(int id, string connectionString) =>
            _dp.LoadData<DmIndexDto, dynamic>("dbo.spGetNumberOfDescendantsById",
                new { id = id }, connectionString);

        public async Task<IEnumerable<DmIndexDto>> GetDmIndexes(string indexNode, int level, string connectionString) 
        {
            _log.LogInformation("Start GetDMIndexes");
            var retryPolicy = Policy
                .Handle<SqlException>()
                .Retry(
                retryCount: 3,
                onRetry: (e, i) => _log.LogInformation("Retrying due to " + e.Message + " Retry " + i + " next.")
                );
            retryPolicy.Execute(() => _ida.WakeUpDatabase(connectionString));
            IEnumerable<DmIndexDto> result = await _dp.LoadData<DmIndexDto, dynamic>("dbo.spGetNumberOfDescendants",
                new { indexnode = indexNode, level = level }, connectionString);
            return result;

        }

        public async Task<IndexDto> GetIndex(int id, string connectionString)
        {
            var results = await _dp.LoadData<IndexDto, dynamic>("dbo.spGetIndexFromId", new { id = id }, connectionString);
            return results.FirstOrDefault();
        }

        public Task<IEnumerable<IndexDto>> GetIndexes(string connectionString) =>
            _dp.LoadData<IndexDto, dynamic>("dbo.spGetIndex", new { }, connectionString);

        public string GetSelectSQL()
        {
            return getSql;
        }

        private async Task<int> Initialize(ConnectParametersDto target, ConnectParametersDto source, BuildIndexParameters idxParms)
        {
            _taxonomy = await _fs.ReadFile("taxonomy", idxParms.Taxonomy);
            if (source.SourceType == "DataBase")
            {
                _sourceAccess = new DBDataAccess();
                var retryPolicy = Policy
                .Handle<SqlException>()
                .Retry(
                retryCount: 3,
                onRetry: (e, i) => _log.LogInformation("Retrying due to " + e.Message + " Retry " + i + " next.")
                );
                retryPolicy.Execute(() => _ida.WakeUpDatabase(source.ConnectionString));
            }
            else
            {
                source.ConnectionString = idxParms.StorageAccount;
                if (source.DataType == "Logs")
                {
                    _sourceAccess = new LASDataAccess(_fs);
                }
                else
                {
                    _sourceAccess = new CSVDataAccess(_fs);
                }
            }
            InitializeIndex(target, source);
            await DeleteIndexes(target.ConnectionString);
            CreateRoot(source);
            return JsonIndexArray.Count;
        }

        private void CreateRoot(ConnectParametersDto source)
        {
            string jsonSource = JsonConvert.SerializeObject(source);
            IndexRootJson indexRootJson = new IndexRootJson
            {
                Source = jsonSource,
                Taxonomy = _taxonomy
            };
            string json = JsonConvert.SerializeObject(indexRootJson);
            myIndex.Add(new IndexData 
            { 
                DataName = "QCPROJECT", 
                DataType = "QCPROJECT", 
                IndexNode = "/", 
                QcLocation = null, 
                JsonDataObject = json }
            );
        }

        private void InitializeIndex(ConnectParametersDto connectionString, ConnectParametersDto source)
        {
            myIndex = new IndexDataCollection();
            _sourceAccess.OpenConnection(source, connectionString);
            _connectionString = connectionString.ConnectionString;
            dataAccessDefs = JsonConvert.DeserializeObject<List<DataAccessDef>>(connectionString.DataAccessDefinition);
            JsonIndexArray = JArray.Parse(_taxonomy);
            _idxData = new List<IndexFileData>();
            foreach (JToken level in JsonIndexArray)
            {
                _idxData.Add(ProcessJTokens(level));
                ProcessIndexArray(JsonIndexArray, level);
            }
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

        private void ProcessIndexArray(JArray JsonIndexArray, JToken parent)
        {
            if (parent["DataObjects"] != null)
            {
                foreach (JToken level in parent["DataObjects"])
                {
                    _idxData.Add(ProcessJTokens(level));
                    ProcessIndexArray(JsonIndexArray, level);
                }
            }
        }

        public async Task<List<ParentIndexNodes>> IndexParent(int parentNodes, string filter)
        {
            List<ParentIndexNodes> nodes = new List<ParentIndexNodes>();
            int nodeId = 0;
            for (int k = 0; k < parentNodes; k++)
            {
                JToken token = JsonIndexArray[k];
                int parentCount = await GetObjectCount(token, k, filter);
                if (parentCount > 0)
                {
                    nodeId++;
                    string strNodeId = $"/{nodeId}/";
                    CreateParentNodeIndex(strNodeId, nodeId);
                    nodes.Add(new ParentIndexNodes()
                    {
                        NodeCount = parentCount,
                        ParentNodeId = strNodeId,
                        Name = (string)token["DataName"],
                        ParentId = nodeId + 1
                    });
                }
            }
            return nodes;
        }

        public void CreateParentNodeIndex(string nodeId, int parentId)
        {
            string parentNodeName = _currentItem.DataName + "s";
            int parentCount = _currentItem.DataTable.Rows.Count;
            if (parentCount > 0)
            {
                myIndex.Add(new IndexData 
                { 
                    DataName = parentNodeName, 
                    DataType = parentNodeName, 
                    IndexNode = nodeId, 
                    QcLocation = null });
            }
        }

        public async Task<int> GetObjectCount(JToken token, int rowNr, string filter)
        {
            string select = "";
            string query = "";
            if (!string.IsNullOrEmpty(filter)) query = " " + filter;
            int objectCount = 0;
            string parentName = (string)token["DataName"];
            _currentItem = _idxData.Find(x => x.DataName == parentName);
            if (string.IsNullOrEmpty(_currentItem.ParentKey))
            {
                select = dataAccessDefs.GetSelectString(_currentItem.DataName);
                if (!string.IsNullOrEmpty(select))
                {
                    _currentItem.DataTable = await _sourceAccess.GetDataTable(select, query, _currentItem.DataName);
                }
                objectCount = _currentItem.DataTable.Rows.Count;
            }
            return objectCount;
        }

        public async Task IndexChildren(int topId, int parentId, string parentNodeId)
        {
            try
            {
                await PopulateIndex(topId, parentId, parentNodeId);
            }
            catch (Exception ex)
            {
                throw new System.Exception($"Error in IndexChildren: {ex.ToString()}");
            }
        }

        public async Task PopulateIndex(int topId, int parentId, string parentNodeId)
        {
            try
            {
                JToken level = JsonIndexArray[topId];
                _currentItem = GetIndexData(level);
                string select = dataAccessDefs.GetSelectString(_currentItem.DataName);
                await GetDataForIndexing(select, "", _currentItem.DataName);
                DataRow dataRow = _currentItem.DataTable.Rows[parentId];
                _location = GetIndexLocation(dataRow);
                parentNodeId = parentNodeId + $"{parentId + 1}/";
                await PopulateIndexItem(dataRow, parentNodeId);
                await PopulateChildIndex(level, parentId, parentNodeId);
            }
            catch (Exception ex)
            {
                throw new System.Exception($"Error in PopulateIndex: {ex.ToString()}");
            }

        }

        private async Task PopulateIndexItem(DataRow dataRow, string parentNodeId)
        {
            double? lat = _location.Latitude;
            double? lon = _location.Longitude;
            string nameAttribute = _currentItem.NameAttribute;
            string name = dataRow[nameAttribute].ToString();
            string keys = dataAccessDefs.GetKeysString(_currentItem.DataName);
            string dataKey = GetDataKey(dataRow, keys);
            string jsonData = dataRow.ConvertDataRowToJson(_currentItem.DataTable);
            string qcLocation = GetQcLocation();
            if (_currentItem.Arrays != null) jsonData = await GetArrays(dataRow, jsonData);
            myIndex.Add(new IndexData
            {
                DataName = name,
                DataType = _currentItem.DataName,
                DataKey = dataKey,
                JsonDataObject = jsonData,
                IndexNode = parentNodeId,
                QcLocation = qcLocation,
                Latitude = lat,
                Longitude = lon
            });
        }

        private async Task PopulateChildIndex(JToken level, int parentId, string parentNodeId)
        {
            if (level["DataObjects"] != null)
            {
                int nodeId = 1;
                foreach (JToken subLevel in level["DataObjects"])
                {
                    _parentItem = GetIndexData(level);
                    _currentItem = GetIndexData(subLevel);
                    string childNodeId = parentNodeId + $"{nodeId}/";
                    if (await CreateChildNodeIndex(subLevel, parentId, childNodeId))
                    {
                        nodeId++;
                        int childCount = _currentItem.DataTable.Rows.Count;
                        DataRow pr = _parentItem.DataTable.Rows[parentId];
                        for (int i = 0; i < childCount; i++)
                        {
                            _parentItem = GetIndexData(level);
                            _currentItem = GetIndexData(subLevel);
                            _location = GetIndexLocation(pr);
                            string indexNode = childNodeId + $"{i + 1}/";
                            await PopulateIndexItem(_currentItem.DataTable.Rows[i], indexNode);
                            await PopulateChildIndex(subLevel, i, indexNode);
                        }
                    }
                }
            }
        }

        private async Task<bool> CreateChildNodeIndex(JToken token, int parentId, string parentNodeId)
        {
            bool childNode = false;
            DataRow pr = _parentItem.DataTable.Rows[parentId];
            string dataName = (string)token["DataName"];
            IndexFileData childItem = _idxData.Find(x => x.DataName == dataName);
            string nodeName = dataName + "s";
            if (string.IsNullOrEmpty(childItem.ParentKey))
            {
            }
            else
            {
                string query = GetParentKey(pr, childItem.ParentKey);
                query = " where " + query;
                string select = dataAccessDefs.GetSelectString(childItem.DataName);
                await GetDataForIndexing(select, query, childItem.DataName);
                if (_currentItem.DataTable.Rows.Count > 0)
                {
                    childNode = true;
                    myIndex.Add(new IndexData { DataName = nodeName, DataType = nodeName, IndexNode = parentNodeId, QcLocation = null });
                }
            }
            return childNode;
        }

        private async Task<string> GetArrays(DataRow dataRow, string inJson)
        {
            string outJson = inJson;
            foreach (JToken array in _currentItem.Arrays)
            {
                string attribute = array["Attribute"].ToString();
                string select = array["Select"].ToString();
                string parentKeys = array["ParentKey"].ToString();
                string query = GetParentKey(dataRow, parentKeys);
                query = " where " + query;
                DataTable dt = await _sourceAccess.GetDataTable(select, query, "");
                if (dt.Rows.Count == 1)
                {
                    string result = dt.Rows[0]["ARRAY"].ToString();
                    JObject dataObject = JObject.Parse(outJson);
                    dataObject[attribute] = result;
                    outJson = dataObject.ToString();
                }
            }
            return outJson;
        }

        private string GetParentKey(DataRow dr, string parentKeyTemplate)
        {
            string parentKey = "";
            string and = "";
            string[] keys = parentKeyTemplate.Split(',');
            foreach (string key in keys)
            {
                int start = key.IndexOf("[") + 1;
                int to = key.IndexOf("]");
                if (start < 0 || to < 0) return "";
                string attribute = key.Substring(start, (to - start));
                string query = key.Substring(0, (start - 1));
                attribute = attribute.Trim();
                string attributeValue = dr[attribute].ToString();
                attributeValue = attributeValue.FixAposInStrings();
                attributeValue = "'" + attributeValue + "'";
                parentKey = parentKey + and + query.Trim() + attributeValue;
                and = " AND ";
            }
            return parentKey;
        }

        private string GetQcLocation()
        {
            string qcLocation;
            if (_location.Latitude == null || _location.Longitude == null)
            {
                qcLocation = null;
            }
            else
            {
                qcLocation = $"POINT ({_location.Longitude} {_location.Latitude})";
            }
            return qcLocation;
        }

        private string GetDataKey(DataRow childRow, string dbKeys)
        {
            string dataKey = "";
            string and = "";
            string[] keys = dbKeys.Split(',');
            foreach (string key in keys)
            {
                string attribute = key.Trim();
                string attributeValue = "'" + childRow[attribute].ToString() + "'";
                dataKey = dataKey + and + key.Trim() + " = " + attributeValue;
                and = " AND ";
            }
            return dataKey;
        }

        private Location GetIndexLocation(DataRow dr)
        {
            Location location = new Location();
            string latitudeAttribute = _currentItem.LatitudeAttribute;
            string longitudeAttribute = _currentItem.LongitudeAttribute;
            if (_currentItem.UseParentLocation)
            {
                latitudeAttribute = _parentItem.LatitudeAttribute;
                longitudeAttribute = _parentItem.LongitudeAttribute;

            }
            location.Latitude = GetLocation(dr, latitudeAttribute);
            location.Longitude = GetLocation(dr, longitudeAttribute);
            if (location.Latitude == -99999.0) location.Latitude = null;
            if (location.Longitude == -99999.0) location.Longitude = null;
            if (location.Latitude != null)
            {
                double loc = (double)location.Latitude;
                if (!loc.Between(-90.0, 90.0))
                {
                    location.Latitude = null;
                }
            }
            if (location.Latitude == null) location.Longitude = null;
            if (location.Longitude == null) location.Latitude = null;
            return location;
        }

        private double GetLocation(DataRow dr, string attribute)
        {
            double location = -99999.0;
            if (!string.IsNullOrEmpty(attribute))
            {
                string strLocation = dr[attribute].ToString();
                if (!string.IsNullOrEmpty(strLocation))
                {
                    Boolean isNumber = double.TryParse(strLocation, out location);
                    if (!isNumber) location = -99999.0;
                }
            }
            return location;
        }

        private IndexFileData GetIndexData(JToken token)
        {
            string dataName = (string)token["DataName"];
            IndexFileData indexObject = _idxData.Find(x => x.DataName == dataName);
            return indexObject;
        }

        private async Task GetDataForIndexing(string select, string query, string dataType)
        {
            if (!String.IsNullOrEmpty(select))
            {
                if (String.IsNullOrEmpty(query))
                {
                    if (_currentItem.DataTable == null)
                    {
                        _currentItem.DataTable = await _sourceAccess.GetDataTable(select, query, _currentItem.DataName);
                    }
                }
                else
                {
                    _currentItem.DataTable = await _sourceAccess.GetDataTable(select, query, _currentItem.DataName);
                }
            }
        }

        public async Task DeleteIndexes(string connectionString)
        {
            string sql = $"DELETE FROM {_table}";
            await _ida.ExecuteSQL(sql, connectionString);
        }

        public async Task InsertIndexes(IndexDataCollection indexes, int parentid, string connectionString)
        {
            string parameterName = "UDIndexTable";
            string sql = "spInsertIndex";
            await _ida.InsertWithUDT(sql, parameterName, indexes, connectionString);
        }

        public async Task<IEnumerable<IndexDto>> QueriedIndexes(string connectionString, string dataType, string qcString)
        {
            string select = getSql;
            if (!string.IsNullOrEmpty(dataType))
            {
                select = select + $" where DATATYPE = '{dataType}'";
                if (!string.IsNullOrEmpty(qcString))
                {
                    select = select + $" and QC_STRING like '%{qcString}%'";
                }
            }
            _log.LogInformation($"QueriedIndexes: Select statement is: {select}");
            IEnumerable<IndexDto> result = await _dp.ReadData<IndexDto>(select, connectionString);
            return result;
        }
    }
}
