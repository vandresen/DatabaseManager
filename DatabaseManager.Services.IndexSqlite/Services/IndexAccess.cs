using DatabaseManager.Services.IndexSqlite.Helpers;
using DatabaseManager.Services.IndexSqlite.Models;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data;
using System.Linq;
using System.Xml.Linq;

namespace DatabaseManager.Services.IndexSqlite.Services
{
    public class IndexAccess : IIndexAccess
    {
        public JArray JsonIndexArray { get; set; }

        private readonly IDataAccess _id;
        private readonly IDataSourceService _ds;
        private readonly IFileStorageService _fs;
        private string _table = "pdo_qc_index";
        private string _selectAttributes = "IndexId, ParentId, DataName, DataType, DataKey, QC_String, UniqKey, JsonDataObject, " +
            "Latitude, Longitude";
        private string getSql;
        private readonly string _databaseFile = @".\mydatabase.db";
        private string _connectionString;
        private List<IndexModel> myIndex;
        private List<DataAccessDef> dataAccessDefs = new List<DataAccessDef>();
        private int _indexId;
        private static List<IndexFileData> _idxData;
        private string _taxonomy;
        private IDataAccess _sourceAccess;
        private IndexFileData _currentItem;
        private Location _location;
        private IndexFileData _parentItem;

        public IndexAccess(IDataAccess id, IDataSourceService ds, IFileStorageService fs)
        {
            _id = id;
            _ds = ds;
            _fs = fs;
            _connectionString = @"Data Source=" + _databaseFile;
            getSql = "Select " + _selectAttributes + " From " + _table;
        }

        public void ClearAllQCFlags(string connectionString)
        {
            throw new NotImplementedException();
        }

        public Task DeleteIndex(int id, string connectionString)
        {
            throw new NotImplementedException();
        }

        public async Task DeleteIndexes(string connectionString)
        {
            string sql = $"DELETE FROM {_table}";
            await _id.DeleteDataSQL<int?>(sql, null, connectionString);
        }

        public Task<IEnumerable<IndexModel>> GetChildrenWithName(string connectionString, string indexNode, string name)
        {
            throw new NotImplementedException();
        }

        public Task<int> GetCount(string connectionString, string query)
        {
            throw new NotImplementedException();
        }

        public DataAccessDef GetDataAccessDefinition()
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<IndexModel>> GetDescendants(int id)
        {
            string sql = $"WITH RECURSIVE IndexHierarchy AS (" +
                " SELECT IndexId, " +
                "DataName, " +
                "ParentId, " + 
                "DataType, " +
                "DataKey, " + 
                "QC_String, " +
                "UniqKey," + 
                "JsonDataObject, " +
                "Latitude, " +
                "Longitude, " +
                "0 AS IndexLevel " +
                "FROM pdo_qc_index " +
                $"WHERE ParentId = {id} " +
                " UNION ALL " +
                " SELECT e.IndexId, " +
                " e.DataName, " +
                " e.ParentId, " +
                " e.DataType, " +
                " e.DataKey, " +
                " e.QC_String, " +
                " e.UniqKey," +
                " e.JsonDataObject, " +
                " e.Latitude, " +
                " e.Longitude, " +
                " IndexLevel + 1 " +
                " FROM pdo_qc_index e, IndexHierarchy ch " +
                " WHERE e.ParentId = ch.IndexId " +
                ") " +
                "SELECT ch.DataName, " +
                " ch.IndexId, " +
                " ch.ParentId, " +
                " ch.DataType, " +
                " ch.DataKey, " +
                " ch.QC_String, " +
                " ch.UniqKey," +
                " ch.JsonDataObject, " +
                " ch.Latitude, " +
                " ch.Longitude, " +
                " IndexLevel " +
                " FROM IndexHierarchy ch " +
                " LEFT JOIN pdo_qc_index e " +
                " ON ch.ParentId = e.IndexId " +
                " ORDER BY ch.IndexLevel, ch.ParentId;";
            IEnumerable<IndexModel> result = await _id.ReadData<IndexModel>(sql, _connectionString);
            return result;
        }

        public async Task<IndexModel> GetIndex(int id)
        {
            string sql = getSql + $" WHERE IndexId = '{id}'";
            var results = await _id.ReadData<IndexModel>(sql, _connectionString);
            return results.FirstOrDefault();
        }

        public async Task<IEnumerable<IndexModel>> GetIndexes()
        {
            //string sql = $"SELECT IndexId, DataName, ST_ASBinary(Locations) as geometry FROM {_table}";
            IEnumerable<IndexModel> result = await _id.ReadData<IndexModel>(getSql, _connectionString);
            return result;
        }

        public Task<IEnumerable<IndexModel>> GetIndexesWithQcStringFromSP(string qcString, string connectionString)
        {
            throw new NotImplementedException();
        }

        public Task<IndexModel> GetIndexFromSP(int id, string connectionString)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<IndexModel>> GetNeighbors(int id)
        {
            string sql = getSql + $" WHERE IndexId = {id}";
            var results = await _id.ReadData<IndexModel>(sql, _connectionString);
            var target = results.FirstOrDefault();
            if (target == null) 
            {
                throw new Exception("Index object does not exist.");
            }
            if (target.NoIndexLocation())
            {
                throw new Exception("Index object does not have a proper location.");
            }
            sql = $"SELECT " +
                _selectAttributes +
                $", Distance(Locations, MakePoint({target.Longitude}, {target.Latitude})) AS Distance " +
                $"FROM {_table} " +
                $" WHERE distance IS NOT NULL AND IndexId != {id} " +
                "ORDER BY Distance " +
                "LIMIT 24";
            IEnumerable<IndexModel> result = await _id.ReadData<IndexModel>(sql, _connectionString);
            return result;
        }

        public Task<IndexModel> GetIndexRoot(string connectionString)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<DmsIndex>> GetNumberOfDescendantsByIdAndLevel(string indexNode, int level, string connectionString)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<DmsIndex>> GetNumberOfDescendantsSP(int id, string connectionString)
        {
            throw new NotImplementedException();
        }

        public string GetSelectSQL()
        {
            throw new NotImplementedException();
        }

        public async Task CreateDatabaseIndex()
        {
            if (!System.IO.File.Exists(_databaseFile))
            {
                using (SqliteConnection connection = new SqliteConnection($"Data Source={_databaseFile}"))
                {
                    connection.Open();
                    using (SqliteCommand command = connection.CreateCommand())
                    {
                        command.CommandText = "CREATE TABLE Dummy (Id INTEGER PRIMARY KEY)";
                        command.ExecuteNonQuery();
                    }
                }
            }
            
            string sql = "SELECT InitSpatialMetaData();" +
                $"DROP TABLE IF EXISTS {_table};" +
                $"CREATE TABLE {_table} " +
                        "(IndexId INTEGER PRIMARY KEY, " +
                        "ParentId INTEGER, " +
                        "DataName TEXT, " +
                        "DataType TEXT, " +
                        "DataKey TEXT, " +
                        "QC_String TEXT, " +
                        "UniqKey TEXT, " +
                        "JsonDataObject TEXT, " +
                        "Latitude REAL, " +
                        "Longitude REAL);" +
                        $"SELECT AddGeometryColumn('{_table}', 'Locations', 4326, 'POINT', 'XY');" +
                        $"SELECT CreateSpatialIndex('{_table}', 'Locations');";
            await _id.ExecuteSQL(sql, _connectionString);
        }

        public async Task InsertSingleIndex(IndexModel indexModel, int parentid, string connectionString)
        {
            string sql = $"INSERT INTO {_table} " +
                "(IndexId, DataName, DataType, JsonDataObject, ParentId, Locations) " +
                "VALUES(@IndexId, @DataName, @DataType, @JsonDataObject, @ParentId, MakePoint(@Longitude, @Latitude, 4326))";
            await _id.SaveDataSQL(sql, indexModel, connectionString);
        }

        public async Task InsertIndexes(List<IndexModel> indexModel, int parentid, string connectionString)
        {
            string sql = $"INSERT INTO {_table} " +
                "(IndexId, DataName, DataType, JsonDataObject, ParentId, Latitude, Longitude, Locations) " +
                "VALUES(@IndexId, @DataName, @DataType, @JsonDataObject, @ParentId, @Latitude, @Longitude, MakePoint(@Longitude, @Latitude, 4326))";
            await _id.SaveDataSQL(sql, indexModel, connectionString);
        }

        public Task UpdateIndex(IndexModel indexModel, string connectionString)
        {
            throw new NotImplementedException();
        }

        public Task UpdateIndexes(List<IndexModel> indexes, string connectionString)
        {
            throw new NotImplementedException();
        }

        public async Task BuildIndex(BuildIndexParameters idxParms)
        {
            ResponseDto dsResponse = await _ds.GetDataSourceByNameAsync<ResponseDto>(idxParms.SourceName);
            ConnectParameters source = JsonConvert.DeserializeObject<ConnectParameters>(Convert.ToString(dsResponse.Result));
            dsResponse = await _ds.GetDataSourceByNameAsync<ResponseDto>(idxParms.TargetName);
            ConnectParameters target = JsonConvert.DeserializeObject<ConnectParameters>(Convert.ToString(dsResponse.Result));

            _fs.SetConnectionString(idxParms.StorageAccount);
            target.DataAccessDefinition = await _fs.ReadFile("connectdefinition", "PPDMDataAccess.json");
            target.ConnectionString = _connectionString;
            
            int parentNodes = await Initialize(target, source, idxParms);

            List<ParentIndexNodes> nodes = await IndexParent(parentNodes, idxParms.Filter);
            for (int j = 0; j < nodes.Count; j++)
            {
                ParentIndexNodes node = nodes[j];
                for (int i = 0; i < node.NodeCount; i++)
                {
                    await IndexChildren(j, i, node.ParentNodeId, node.ParentId);
                }
            }
            await InsertIndexes(myIndex, 0, _connectionString);
        }

        private async Task IndexChildren(int topId, int parentId, string parentNodeId, int parentIndexId)
        {
            try
            {
                await PopulateIndex(topId, parentId, parentNodeId, parentIndexId);
            }
            catch (Exception ex)
            {
                throw new System.Exception($"Error in IndexChildren: {ex.ToString()}");
            }
        }

        public async Task PopulateIndex(int topId, int parentId, string parentNodeId, int parentIndexId)
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
                PopulateIndexItem(dataRow, parentNodeId, parentIndexId);
                parentIndexId = _indexId;
                await PopulateChildIndex(level, parentId, parentNodeId, parentIndexId);
            }
            catch (Exception ex)
            {
                throw new System.Exception($"Error in PopulateIndex: {ex.ToString()}");
            }

        }

        private async Task PopulateChildIndex(JToken level, int parentId, string parentNodeId, int parentIndexId)
        {
            if (level["DataObjects"] != null)
            {
                int nodeId = 1;
                foreach (JToken subLevel in level["DataObjects"])
                {
                    _parentItem = GetIndexData(level);
                    _currentItem = GetIndexData(subLevel);
                    string childNodeId = parentNodeId + $"{nodeId}/";
                    if (await CreateChildNodeIndex(subLevel, parentId, childNodeId, parentIndexId))
                    {
                        parentIndexId = _indexId;
                        nodeId++;
                        int childCount = _currentItem.DataTable.Rows.Count;
                        DataRow pr = _parentItem.DataTable.Rows[parentId];
                        for (int i = 0; i < childCount; i++)
                        {
                            _parentItem = GetIndexData(level);
                            _currentItem = GetIndexData(subLevel);
                            _location = GetIndexLocation(pr);
                            string indexNode = childNodeId + $"{i + 1}/";
                            PopulateIndexItem(_currentItem.DataTable.Rows[i], indexNode, parentIndexId);
                            await PopulateChildIndex(subLevel, i, indexNode, parentIndexId);
                        }
                    }
                }
            }
        }

        private void PopulateIndexItem(DataRow dataRow, string parentNodeId, int parentIndexId)
        {
            double? lat = _location.Latitude;
            double? lon = _location.Longitude;
            string nameAttribute = _currentItem.NameAttribute;
            string name = dataRow[nameAttribute].ToString();
            string keys = dataAccessDefs.GetKeysString(_currentItem.DataName);
            string dataKey = GetDataKey(dataRow, keys);
            string jsonData = dataRow.ConvertDataRowToJson(_currentItem.DataTable);
            string qcLocation = GetQcLocation();
            if (_currentItem.Arrays != null) jsonData = GetArrays(dataRow, jsonData);
            _indexId++;
            myIndex.Add(new IndexModel
            {
                IndexId = _indexId,
                DataName = name,
                DataType = _currentItem.DataName,
                DataKey = dataKey,
                JsonDataObject = jsonData,
                IndexNode = parentNodeId,
                ParentId = parentIndexId,
                Latitude = lat,
                Longitude = lon
            });
        }

        private string GetArrays(DataRow dataRow, string inJson)
        {
            string outJson = inJson;
            foreach (JToken array in _currentItem.Arrays)
            {
                string attribute = array["Attribute"].ToString();
                string select = array["Select"].ToString();
                string parentKeys = array["ParentKey"].ToString();
                string query = GetParentKey(dataRow, parentKeys);
                query = " where " + query;
                //DataTable dt = dbConn.GetDataTable(select, query);
                //if (dt.Rows.Count == 1)
                //{
                //    string result = dt.Rows[0]["ARRAY"].ToString();
                //    JObject dataObject = JObject.Parse(outJson);
                //    dataObject[attribute] = result;
                //    outJson = dataObject.ToString();
                //}
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
                    bool isNumber = double.TryParse(strLocation, out location);
                    if (!isNumber) location = -99999.0;
                }
            }
            return location;
        }

        private async Task GetDataForIndexing(string select, string query, string dataType)
        {
            if (!string.IsNullOrEmpty(select))
            {
                if (string.IsNullOrEmpty(query))
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

        private IndexFileData GetIndexData(JToken token)
        {
            string dataName = (string)token["DataName"];
            IndexFileData indexObject = _idxData.Find(x => x.DataName == dataName);
            return indexObject;
        }

        private async Task<int> Initialize(ConnectParameters target, ConnectParameters source, BuildIndexParameters idxParms)
        {
            _taxonomy = await _fs.ReadFile("taxonomy", idxParms.TaxonomyFile);
            if (source.SourceType == "DataBase")
            {
                //iBuilder = new IndexBuilder(new DBDataAccess(_db));
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

        private void CreateRoot(ConnectParameters source)
        {
            string jsonSource = "CVS";
            IndexRootJson indexRootJson = new IndexRootJson
            {
                Source = jsonSource,
                Taxonomy = _taxonomy
            };
            string json = JsonConvert.SerializeObject(indexRootJson);
            myIndex.Add(new IndexModel
            {
                IndexId = _indexId,
                DataName = "QCPROJECT",
                DataType = "QCPROJECT",
                IndexNode = "/",
                JsonDataObject = json
            });
        }

        private void InitializeIndex(ConnectParameters connectionString, ConnectParameters source)
        {
            _indexId = 1;
            myIndex = new List<IndexModel>();
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

        static void ProcessIndexArray(JArray JsonIndexArray, JToken parent)
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

        public void CreateParentNodeIndex(string nodeId, int parentId)
        {
            string parentNodeName = _currentItem.DataName + "s";
            int parentCount = _currentItem.DataTable.Rows.Count;
            if (parentCount > 0)
            {
                _indexId++;
                myIndex.Add(new IndexModel
                {
                    IndexId = _indexId,
                    DataName = parentNodeName,
                    ParentId = parentId,
                    DataType = parentNodeName,
                    IndexNode = nodeId
                });
            }
        }

        private async Task<bool> CreateChildNodeIndex(JToken token, int parentId, string parentNodeId, int parentIndexId)
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
                    _indexId++;
                    myIndex.Add(new IndexModel
                    {
                        IndexId = _indexId,
                        DataName = nodeName,
                        DataType = nodeName,
                        IndexNode = parentNodeId,
                        ParentId = parentIndexId
                    });
                }
            }
            return childNode;
        }
    }
}
