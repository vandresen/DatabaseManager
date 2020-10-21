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
        private DbUtilities dbConn;
        private List<DataAccessDef> dataAccessDefs = new List<DataAccessDef>();
        private static List<IndexFileData> _idxData;
        private IndexDataCollection myIndex;
        private IndexFileData _currentItem;
        private Location _location;
        private IndexFileData _parentItem;
        public JArray JsonIndexArray { get; set; }

        public IndexBuilder()
        {
            dbConn = new DbUtilities();
        }

        public void InitializeIndex(ConnectParameters connectionString, string jsonTaxonomy, string jsonConnectDef)
        {
            myIndex = new IndexDataCollection();
            dbConn.OpenConnection(connectionString);
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
            dbConn.DBDelete(table);
            myIndex.Add(new IndexData { DataName = "QCPROJECT", DataType = "QCPROJECT", IndexNode = "/", QcLocation = null });
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
                    _currentItem.DataTable = dbConn.GetDataTable(select, query);
                }
                objectCount = _currentItem.DataTable.Rows.Count;
            }
            return objectCount;
        }

        public void CreateParentNodeIndex(string nodeId)
        {
            string parentNodeName = _currentItem.DataName + "s";
            int parentCount = _currentItem.DataTable.Rows.Count;
            if (parentCount > 0)
            {
                myIndex.Add(new IndexData { DataName = parentNodeName, DataType = parentNodeName, IndexNode = nodeId, QcLocation = null });
            }
        }

        public void PopulateIndex(int topId, int parentId, string parentNodeId)
        {
            JToken level = JsonIndexArray[topId];
            _currentItem = GetIndexData(level);
            string select = dataAccessDefs.GetSelectString(_currentItem.DataName);
            GetDataForIndexing(select, "");
            DataRow dataRow = _currentItem.DataTable.Rows[parentId];
            _location = GetIndexLocation(dataRow);
            parentNodeId = parentNodeId + $"{parentId + 1}/";
            PopulateIndexItem(dataRow, parentNodeId);
            PopulateChildIndex(level, parentId, parentNodeId);
        }

        public void CloseIndex()
        {
            dbConn.InsertUserDefinedTable(myIndex);
            dbConn.CloseConnection();
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

        private IndexFileData GetIndexData(JToken token)
        {
            string dataName = (string)token["DataName"];
            IndexFileData indexObject = _idxData.Find(x => x.DataName == dataName);
            return indexObject;
        }

        private void GetDataForIndexing(string select, string query)
        {
            if (!String.IsNullOrEmpty(select))
            {
                _currentItem.DataTable = dbConn.GetDataTable(select, query);
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
            location.Longitude = GetLocation(dr, longitudeAttribute);
            if (location.Latitude == -99999.0) location.Latitude = null;
            if (location.Longitude == -99999.0) location.Longitude = null;
            if (location.Latitude != null)
            {
                double loc = (double)location.Latitude;
                if (!Common.Between(loc, -90.0, 90.0))
                {
                    location.Latitude = null;
                }
            }
            if (location.Latitude == null) location.Longitude = null;
            if (location.Longitude == null) location.Latitude = null;
            return location;
        }

        private void PopulateIndexItem(DataRow dataRow, string parentNodeId)
        {
            double? lat = _location.Latitude;
            double? lon = _location.Longitude;
            string nameAttribute = _currentItem.NameAttribute;
            string name = dataRow[nameAttribute].ToString();
            string keys = dataAccessDefs.GetKeysString(_currentItem.DataName);
            string dataKey = GetDataKey(dataRow, keys);
            string jsonData = Common.ConvertDataRowToJson(dataRow, _currentItem.DataTable);
            string qcLocation = GetQcLocation();
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

        private void PopulateChildIndex(JToken level, int parentId, string parentNodeId)
        {
            if (level["DataObjects"] != null)
            {
                int nodeId = 1;
                foreach (JToken subLevel in level["DataObjects"])
                {
                    _parentItem = GetIndexData(level);
                    _currentItem = GetIndexData(subLevel);
                    string childNodeId = parentNodeId + $"{nodeId}/";
                    if (CreateChildNodeIndex(subLevel, parentId, childNodeId))
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
                            PopulateIndexItem(_currentItem.DataTable.Rows[i], indexNode);
                            PopulateChildIndex(subLevel, i, indexNode);
                        }
                    }
                }
            }
        }

        private bool CreateChildNodeIndex(JToken token, int parentId, string parentNodeId)
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
                GetDataForIndexing(select, query);
                if (_currentItem.DataTable.Rows.Count > 0)
                {
                    childNode = true;
                    myIndex.Add(new IndexData { DataName = nodeName, DataType = nodeName, IndexNode = parentNodeId, QcLocation = null });
                }
            }
            return childNode;
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
    }
}
