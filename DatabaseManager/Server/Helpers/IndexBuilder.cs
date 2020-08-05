using DatabaseManager.Server.Entities;
using DatabaseManager.Server.Extensions;
using DatabaseManager.Shared;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace DatabaseManager.Server.Helpers
{
    public class IndexBuilder
    {
        private DbUtilities _dbConn;
        private int _rootSeqNo;
        private static List<IndexFileData> _idxData;
        private IndexFileData _currentItem;
        private IndexFileData _parentItem;
        private Location _location;
        private List<DataAccessDef> dataAccessDefs = new List<DataAccessDef>();

        public JArray JsonIndexArray { get; set; }

        public IndexBuilder()
        {
            _dbConn = new DbUtilities();
        }

        public void CloseIndex()
        {
            _dbConn.CloseConnection();
        }

        public void InitializeIndex(ConnectParameters connectionString, string jsonTaxonomy, string jsonConnectDef)
        {
            _dbConn.OpenConnection(connectionString);
            dataAccessDefs = JsonConvert.DeserializeObject<List<DataAccessDef>>(jsonConnectDef);
            JsonIndexArray = JArray.Parse(jsonTaxonomy);
            _idxData = new List<IndexFileData>();
            foreach (JToken level in JsonIndexArray)
            {
                _idxData.Add(ProcessJTokens(level));
                ProcessIndexArray(JsonIndexArray, level);
            }
        }

        public void CreateRoot()
        {
            string table = "pdo_qc_index";
            _dbConn.DBDelete(table);
            _rootSeqNo = _dbConn.InsertIndex(-1, "", "", "", "", 0, 0);
        }

        public int GetObjectCount(JToken token, int rowNr)
        {
            string select = "";
            string query = "";
            int objectCount = 0;
            string parentName = (string)token["DataName"];
            _currentItem = _idxData.Find(x => x.DataName == parentName);
            if (string.IsNullOrEmpty(_currentItem.ParentKey))
            {
                select = dataAccessDefs.GetSelectString(_currentItem.DataName);
                if (!String.IsNullOrEmpty(select))
                {
                    _currentItem.DataTable = _dbConn.GetDataTable(select, query);
                }
                objectCount = _currentItem.DataTable.Rows.Count;
            }
            return objectCount;
        }

        public int CreateParentNodeIndex()
        {
            int parentNodeId = 0;
            string parentNodeName = _currentItem.DataName + "s";
            int parentCount = _currentItem.DataTable.Rows.Count;
            if (parentCount > 0)
            {
                parentNodeId = _dbConn.InsertIndex(_rootSeqNo, parentNodeName, parentNodeName, "", "", 0.0, 0.0);
            }
            return parentNodeId;
        }

        public void PopulateIndex(int topId, int parentId, int parentNodeId)
        {
            JToken level = JsonIndexArray[topId];
            _currentItem = GetIndexData(level);
            string select = dataAccessDefs.GetSelectString(_currentItem.DataName);
            GetDataForIndexing(select, "");
            DataRow dataRow = _currentItem.DataTable.Rows[parentId];
            _location = GetIndexLocation(dataRow);
            int levelId = PopulateIndexItem(dataRow, parentNodeId);
            PopulateChildIndex(level, parentId, levelId);
        }

        private void PopulateChildIndex(JToken level, int parentId, int parentNodeId)
        {
            if (level["DataObjects"] != null)
            {
                foreach (JToken subLevel in level["DataObjects"])
                {
                    _parentItem = GetIndexData(level);
                    _currentItem = GetIndexData(subLevel);
                    int nodeId = CreateChildNodeIndex(subLevel, parentId, parentNodeId);
                    if (nodeId > 0)
                    {
                        int childCount = _currentItem.DataTable.Rows.Count;
                        DataRow pr = _parentItem.DataTable.Rows[parentId];
                        for (int i = 0; i < childCount; i++)
                        {
                            _parentItem = GetIndexData(level);
                            _currentItem = GetIndexData(subLevel);
                            _location = GetIndexLocation(pr);
                            int levelId = PopulateIndexItem(_currentItem.DataTable.Rows[i], nodeId);
                            PopulateChildIndex(subLevel, i, levelId);
                        }

                    }
                }
            }
        }

        private int CreateChildNodeIndex(JToken token, int parentId, int parentNodeId)
        {
            int childNodeId = 0;
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
                GetDataForIndexing(select, query);
                if (_currentItem.DataTable.Rows.Count > 0)
                {
                    childNodeId = _dbConn.InsertIndex(parentNodeId, nodeName, nodeName, "", "", 0.0, 0.0);
                }
            }
            return childNodeId;
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
                attributeValue = Common.FixAposInStrings(attributeValue);
                attributeValue = "'" + attributeValue + "'";
                parentKey = parentKey + and + query.Trim() + attributeValue;
                and = " AND ";
            }
            return parentKey;
        }

        private IndexFileData GetIndexData(JToken token)
        {
            string dataName = (string)token["DataName"];
            IndexFileData indexObject = _idxData.Find(x => x.DataName == dataName);
            return indexObject;
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

        private static IndexFileData ProcessJTokens(JToken token)
        {
            IndexFileData idxDataObject = new IndexFileData();
            idxDataObject.DataName = (string)token["DataName"];
            idxDataObject.NameAttribute = token["NameAttribute"]?.ToString();
            idxDataObject.LatitudeAttribute = token["LatitudeAttribute"]?.ToString();
            idxDataObject.LongitudeAttribute = token["LongitudeAttribute"]?.ToString();
            idxDataObject.ParentKey = token["ParentKey"]?.ToString();
            if (token["UseParentLocation"] != null) idxDataObject.UseParentLocation = (Boolean)token["UseParentLocation"];
            return idxDataObject;
        }

        private void GetDataForIndexing(string select, string query)
        {
            if (!String.IsNullOrEmpty(select))
            {
                _currentItem.DataTable = _dbConn.GetDataTable(select, query);
            }
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
            if (location.Latitude != -99999.0)
            {
                if (!Common.Between(location.Latitude, 90.0, -90.0))
                {
                    location.Latitude = -99999.0;
                }
            }
            location.Longitude = GetLocation(dr, longitudeAttribute);

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

        private int PopulateIndexItem(DataRow dataRow, int parentNodeId)
        {
            double lat = _location.Latitude;
            double lon = _location.Longitude;
            string nameAttribute = _currentItem.NameAttribute;
            string name = dataRow[nameAttribute].ToString();
            string keys = dataAccessDefs.GetKeysString(_currentItem.DataName);
            string dataKey = GetDataKey(dataRow, keys);
            string jsonData = Common.ConvertDataRowToJson(dataRow, _currentItem.DataTable);
            int indexId = _dbConn.InsertIndex(parentNodeId, name, _currentItem.DataName, dataKey, jsonData, lat, lon);
            return indexId;
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
    }
}
