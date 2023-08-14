using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using DatabaseManager.Common.Data;
using DatabaseManager.Common.DBAccess;
using DatabaseManager.Shared;
using DatabaseManager.Common.Services;
using Microsoft.Extensions.Configuration;
using DatabaseManager.Common.Entities;
using System.Data;
using Microsoft.Data.SqlClient;

namespace DatabaseManager.Common.Helpers
{
    public class IndexToDatabaseTransfer
    {
        private readonly ILogger _log;
        private string _connectionString;
        private readonly IIndexDBAccess _indexData;
        private readonly DapperDataAccess _dp;
        private IADODataAccess _db;
        private readonly IFileStorageServiceCommon _fileStorage;
        private List<ReferenceTable> _references;
        private List<DataAccessDef> _accessDefs;
        private DataAccessDef _objectAccessDef;
        private JArray JsonIndexArray { get; set; }

        public IndexToDatabaseTransfer(ILogger log, string connectionString)
        {
            _log = log;
            _connectionString = connectionString;
            _dp = new DapperDataAccess();
            _indexData = new IndexDBAccess(_dp);
            _db = new ADODataAccess();
            var builder = new ConfigurationBuilder();
            IConfiguration configuration = builder.Build();
            _fileStorage = new AzureFileStorageServiceCommon(configuration);
            _fileStorage.SetConnectionString(connectionString);
        }

        public async Task<List<string>> GetDataObjectList(ConnectParameters connector)
        {
            IndexModel root = await _indexData.GetIndexRoot(connector.ConnectionString);
            IndexRootJson rootJson = JsonConvert.DeserializeObject<IndexRootJson>(root.JsonDataObject);
            JsonIndexArray = JArray.Parse(rootJson.Taxonomy);
            List<string> dataObjects= new List<string>();
            foreach (JToken level in JsonIndexArray)
            {
                dataObjects.Add((string)level["DataName"]);
                dataObjects = ProcessIndexArray(JsonIndexArray, level, dataObjects);
            }
            return dataObjects;
        }

        public async Task TransferDataObject(ConnectParameters source, ConnectParameters target, string dataObjectType)
        {
            _log.LogInformation($"Setting up for index to database transfer");
            string referenceJson = await _fileStorage.ReadFile("connectdefinition", "PPDMReferenceTables.json");
            _references = JsonConvert.DeserializeObject<List<ReferenceTable>>(referenceJson);
            _accessDefs = JsonConvert.DeserializeObject<List<DataAccessDef>>(target.DataAccessDefinition);
            _objectAccessDef = _accessDefs.FirstOrDefault(x => x.DataType == dataObjectType);

            IEnumerable<IndexModel> indexes = await _indexData.GetIndexesWithDataType(source.ConnectionString, dataObjectType);
            await SyncObjectDelete(indexes, target.ConnectionString);
            await SyncObjectUpdateInsert(indexes, target.ConnectionString);
        }

        private async Task SyncObjectUpdateInsert(IEnumerable<IndexModel> indexes, string connectionString)
        {
            string json = indexes.FirstOrDefault()?.JsonDataObject;
            string datatype = indexes.FirstOrDefault()?.DataType;
            string[] attributes = Common.GetAttributes(_objectAccessDef.Select);
            JObject jsonObject = JObject.Parse(json);

            // Extract the JSON properties and their types
            Dictionary<string, Type> columnDefinitions = new Dictionary<string, Type>();
            foreach (var property in jsonObject.Properties())
            {
                JTokenType propertyType = property.Value.Type;
                if (attributes.Contains(property.Name))
                {
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
                if (!string.IsNullOrEmpty(json))
                {
                    jsonObject = JObject.Parse(json);
                    DataRow dataRow = dataTable.NewRow();
                    foreach (var property in jsonObject.Properties())
                    {
                        if (attributes.Contains(property.Name))
                        {
                            if (property.Value.Type != JTokenType.Null)
                            {
                                dataRow[property.Name] = property.Value.ToObject(columnDefinitions[property.Name]);
                            }
                        }
                    }
                    dataTable.Rows.Add(dataRow);
                }
            }

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

                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        connection.Open();
                        string insertColumns;
                        string selectColumns;
                        string condition;
                        //string insertQuery = $"INSERT INTO {refTable.Table} ";
                        if (valueAttribute == refTable.KeyAttribute)
                        {
                            insertColumns = $"{refTable.KeyAttribute}";
                            selectColumns = $"@Value";
                            condition = $"{refTable.KeyAttribute} = @Value";
                        }
                        else
                        {
                            insertColumns = $"{refTable.KeyAttribute}, {valueAttribute}";
                            selectColumns = $"@Value, @Value";
                            condition = $"{refTable.KeyAttribute} = @Value";
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
                string jsonData = index.JsonDataObject;
                if (string.IsNullOrEmpty(jsonData))
                {
                    if (_objectAccessDef != null)
                    {
                        string select = _objectAccessDef.Select;
                        string dataTable = Common.GetTable(select);
                        string dataQuery = "where " + index.DataKey;
                        string sql = "Delete from " + dataTable + " " + dataQuery;
                        _db.ExecuteSQL(sql, conncetionString);
                    }
                }
            }
        }

        private List<string> ProcessIndexArray(JArray JsonIndexArray, JToken parent, List<string> idxData)
        {
            List<string> result = idxData;
            if (parent["DataObjects"] != null)
            {
                foreach (JToken level in parent["DataObjects"])
                {
                    result.Add((string)level["DataName"]);
                    result = ProcessIndexArray(JsonIndexArray, level, result);
                }
            }
            return result;
        }
    }
}
