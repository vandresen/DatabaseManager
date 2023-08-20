using DatabaseManager.Services.DataTransfer.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Runtime.Intrinsics.Arm;
using System.ComponentModel.DataAnnotations;
using DatabaseManager.Services.DataTransfer.Extensions;
using Microsoft.Data.SqlClient;
using System.Data;
using DatabaseManager.Services.DataTransfer.Helpers;
using System.Text;

namespace DatabaseManager.Services.DataTransfer.Services
{
    public class IndexDataTransfer : IDataTransfer
    {
        private readonly IDatabaseAccess _dbAccess;
        private readonly ILogger _log;
        private readonly IFileStorageService _fileStorage;
        private List<ReferenceTable> _references;
        private List<DataAccessDef> _accessDefs;
        private DataAccessDef _objectAccessDef;
        private string getSql = "Select IndexId, IndexNode.ToString() AS TextIndexNode, " +
            "IndexLevel, DataName, DataType, DataKey, QC_String, UniqKey, JsonDataObject, " +
            "Latitude, Longitude " +
            "from pdo_qc_index";

        private JArray JsonIndexArray { get; set; }

        public IndexDataTransfer(ILogger log, IFileStorageService fileStorage)
        {
            _fileStorage = fileStorage;
            _dbAccess = new DatabaseAccess();
            _log = log;
        }

        public async Task CopyData(TransferParameters transferParameters, ConnectParametersDto sourceConnector, ConnectParametersDto targetConnector, string referenceJson)
        {
            _log.LogInformation($"Setting up for index to database transfer");
            _references = JsonConvert.DeserializeObject<List<ReferenceTable>>(referenceJson);
            _accessDefs = JsonConvert.DeserializeObject<List<DataAccessDef>>(targetConnector.DataAccessDefinition);
            _objectAccessDef = _accessDefs.FirstOrDefault(x => x.DataType == transferParameters.Table);

            IEnumerable<IndexModel> indexes = await GetIndexesWithDataType(transferParameters.Table, sourceConnector.ConnectionString);
            if (indexes.Count() > 0)
            {
                await SyncObjectDelete(indexes, targetConnector.ConnectionString);
                await SyncObjectUpdateInsert(indexes, targetConnector.ConnectionString);
            }
        }

        public void DeleteData(ConnectParametersDto source, string table)
        {
            throw new NotImplementedException();
        }

        public async Task<List<string>> GetContainers(ConnectParametersDto source)
        {
            IndexModel root = await GetIndexRoot(source.ConnectionString);
            IndexRootJson rootJson = JsonConvert.DeserializeObject<IndexRootJson>(root.JsonDataObject);
            JsonIndexArray = JArray.Parse(rootJson.Taxonomy);
            List<string> dataObjects = new List<string>();
            foreach (JToken level in JsonIndexArray)
            {
                dataObjects.Add((string)level["DataName"]);
                dataObjects = ProcessIndexArray(JsonIndexArray, level, dataObjects);
            }
            return dataObjects;
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

        private async Task<IndexModel> GetIndexRoot(string connectionString)
        {
            IndexModel idx = new();
            var results = await _dbAccess.LoadData<IndexModel, dynamic>("dbo.spGetIndexWithINDEXNODE", new { query = '/' }, connectionString);
            idx =  results.FirstOrDefault();
            return idx;
        }

        private async Task<IEnumerable<IndexModel>> GetIndexesWithDataType(string dataType, string connectionString)
        {
            string sql = getSql + $" WHERE DATATYPE = '{dataType}'";
            IEnumerable<IndexModel> result = await _dbAccess.ReadData<IndexModel>(sql, connectionString);

            return result;
        }

        private async Task SyncObjectUpdateInsert(IEnumerable<IndexModel> indexes, string connectionString)
        {
            string[] attributes = _objectAccessDef.Select.GetAttributes();
            string dataModel = await _fileStorage.ReadFile("ppdm39", "TAB.sql");
            string datatype = indexes.FirstOrDefault()?.DataType;
            string[] referenceAttributeArray = _references
                .Where(obj => obj.DataType == datatype)
                .Select(obj => obj.ReferenceAttribute)
                .ToArray();
            DataTable dataTable = Common.NewDataTable(datatype, dataModel, _accessDefs);

            string tempTableName = "#TempTable";
            StringBuilder createTempTableQuery = new StringBuilder($"CREATE TABLE {tempTableName} (");
            foreach (DataColumn column in dataTable.Columns)
            {
                string columnName = column.ColumnName;
                string dataType = Common.GetSqlDbTypeString(column.DataType); // Map .NET data types to SQL Server data types
                string columnDefinition = $"{columnName} {dataType}";
                createTempTableQuery.Append(columnDefinition).Append(", ");
            }

            // Remove the trailing comma and space
            createTempTableQuery.Length -= 2;
            createTempTableQuery.Append(")");

            string json = indexes.FirstOrDefault()?.JsonDataObject; // Delete later
            JObject jsonObject = JObject.Parse(json); //Delete later

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
                                DataColumn column = dataRow.Table.Columns[property.Name];
                                Type dataType = column.DataType;
                                dataRow[property.Name] = Common.ConvertJsonValueToDataTable(property.Value, dataType);
                            }
                            else
                            {
                                if(referenceAttributeArray.Contains(property.Name))
                                {
                                    dataRow[property.Name] = "UNKNOWN";
                                }
                            }
                        }
                    }
                    dataTable.Rows.Add(dataRow);
                }
            }

            // Extract the JSON properties and their types
            //Dictionary<string, Type> columnDefinitions = new Dictionary<string, Type>();
            //foreach (var property in jsonObject.Properties())
            //{
            //    JTokenType propertyType = property.Value.Type;
            //    if (attributes.Contains(property.Name))
            //    {
            //        Type columnType;
            //        switch (propertyType)
            //        {
            //            case JTokenType.String:
            //                columnType = typeof(string);
            //                break;
            //            case JTokenType.Integer:
            //                columnType = typeof(int);
            //                break;
            //            case JTokenType.Float:
            //                columnType = typeof(double);
            //                break;
            //            case JTokenType.Boolean:
            //                columnType = typeof(bool);
            //                break;
            //            default:
            //                columnType = typeof(string); // Default to string if the type is not recognized
            //                break;
            //        }

            //        columnDefinitions.Add(property.Name, columnType);
            //    }
            //}

            // Create the temporary table in the SQL Server database
            //string tempTableName = "#TempTable";
            //string createTableQuery = $"CREATE TABLE {tempTableName} (";

            //foreach (var column in columnDefinitions)
            //{
            //    createTableQuery += $"{column.Key} {Common.GetSqlDbTypeString(column.Value)}, ";
            //}

            //createTableQuery = createTableQuery.TrimEnd(',', ' ') + ")";

            // Convert the JSON to a DataTable
            //DataTable dataTable = new DataTable();
            //foreach (var column in columnDefinitions)
            //{
            //    dataTable.Columns.Add(column.Key, column.Value);
            //}

            // Deserialize JSON and insert data into the DataTable
            //foreach (var index in indexes)
            //{
            //    json = index.JsonDataObject;
            //    if (!string.IsNullOrEmpty(json))
            //    {
            //        jsonObject = JObject.Parse(json);
            //        DataRow dataRow = dataTable.NewRow();
            //        foreach (var property in jsonObject.Properties())
            //        {
            //            if (attributes.Contains(property.Name))
            //            {
            //                if (property.Value.Type != JTokenType.Null)
            //                {
            //                    dataRow[property.Name] = property.Value.ToObject(columnDefinitions[property.Name]);
            //                }
            //            }
            //        }
            //        dataTable.Rows.Add(dataRow);
            //    }
            //}

            LoadNewReferences(datatype, connectionString, dataTable);

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                using (SqlCommand createTableCommand = new SqlCommand(createTempTableQuery.ToString(), connection))
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
            string table = dataTypeSql.GetTable();
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
                        string dataTable = select.GetTable();
                        string dataQuery = "where " + index.DataKey;
                        string sql = "Delete from " + dataTable + " " + dataQuery;
                        _dbAccess.ExecuteSQL(sql, conncetionString);
                    }
                }
            }
        }
    }
}
