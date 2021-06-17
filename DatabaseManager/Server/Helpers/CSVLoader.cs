using CsvHelper;
using CsvHelper.Configuration;
using DatabaseManager.Server.Entities;
using DatabaseManager.Server.Extensions;
using DatabaseManager.Server.Services;
using DatabaseManager.Shared;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.SqlClient;
using Microsoft.VisualBasic.FileIO;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Server.Helpers
{
    public class CSVLoader
    {
        private List<ReferenceTable> _references = new List<ReferenceTable>();
        private List<DataAccessDef> _dataDef = new List<DataAccessDef>();
        private List<CSVAccessDef> _csvDef = new List<CSVAccessDef>();
        private Dictionary<string, string> duplicates =
                       new Dictionary<string, string>();
        DataAccessDef dataAccess;
        private string connectionString;
        private string tableDataType;
        private DataTable dt = new DataTable();
        SqlDataAdapter dataAdapter;
        private readonly IFileStorageService fileStorageService;

        public CSVLoader(IFileStorageService fileStorageService)
        {
            this.fileStorageService = fileStorageService;

        }

        public async Task<DataTable> GetCSVTable(ConnectParameters source, ConnectParameters target, string indexDataType)
        {
            DataTable result = new DataTable();
            if (dt.Rows.Count == 0)
            {
                DateTime timeStart = DateTime.Now;
                DateTime timeEnd;
                TimeSpan diff;

                connectionString = target.ConnectionString;
                dt = new DataTable();
                string accessJson = await fileStorageService.ReadFile("connectdefinition", "PPDMDataAccess.json");
                _dataDef = JsonConvert.DeserializeObject<List<DataAccessDef>>(accessJson);
                string referenceJson = await fileStorageService.ReadFile("connectdefinition", "PPDMReferenceTables.json");
                _references = JsonConvert.DeserializeObject<List<ReferenceTable>>(referenceJson);
                string csvJson = await fileStorageService.ReadFile("connectdefinition", "CSVDataAccess.json");
                _csvDef = JsonConvert.DeserializeObject<List<CSVAccessDef>>(csvJson);

                //Console.WriteLine("start reading csv file");
                string csvText = await fileStorageService.ReadFile(source.Catalog, source.FileName);
                timeEnd = DateTime.Now;
                diff = timeEnd - timeStart;
                //Console.WriteLine($"Time span, read all definitions files: {diff}");

                string dataType = source.DataType.Remove(source.DataType.Length - 1, 1);
                dataAccess = _dataDef.First(x => x.DataType == dataType);
                dt = NewDataTable(dataType);
                tableDataType = dataType;

                int headerLines = 1;
                int wellCounter = 0;

                CSVAccessDef csvAccess = _csvDef.First(x => x.DataType == dataType);
                Dictionary<string, int> attributes = csvAccess.Mappings.ToDictionary();
                Dictionary<string, string> constants = csvAccess.Constants.ToStringDictionary();

                byte[] byteArray = Encoding.ASCII.GetBytes(csvText);
                MemoryStream csvStream = new MemoryStream(byteArray);

                using (TextFieldParser csvParser = new TextFieldParser(csvStream))
                {
                    csvParser.CommentTokens = new string[] { "#" };
                    csvParser.SetDelimiters(new string[] { "," });
                    csvParser.HasFieldsEnclosedInQuotes = true;

                    for (int i = 0; i < headerLines; i++)
                    {
                        string line = csvParser.ReadLine();
                    }

                    while (!csvParser.EndOfData)
                    {
                        wellCounter++;
                        if ((wellCounter % 1000) == 0)
                        {
                            var tmp = wellCounter;
                        }
                        string[] fields = csvParser.ReadFields();
                        string key = GetDataKey(fields, attributes);
                        string duplicateKey = GetDuplicateKey(fields, attributes);
                        if (string.IsNullOrEmpty(key))
                        {
                            //Console.WriteLine($"Well {wellCounter} has an empty key");
                        }
                        else
                        {
                            if (duplicates.ContainsKey(duplicateKey))
                            {
                                DataRow[] rows = dt.Select(key);
                                rows[0] = InsertCSVRow(rows[0], attributes, fields, constants);
                                rows[0]["ROW_CHANGED_DATE"] = DateTime.Now.ToString("yyyy-MM-dd");
                            }
                            else
                            {
                                DataRow newRow = dt.NewRow();
                                newRow = InsertCSVRow(newRow, attributes, fields, constants);
                                newRow["ROW_CREATED_DATE"] = DateTime.Now.ToString("yyyy-MM-dd");
                                newRow["ROW_CHANGED_DATE"] = DateTime.Now.ToString("yyyy-MM-dd");
                                dt.Rows.Add(newRow);
                                duplicates.Add(duplicateKey, "");
                            }
                        }
                    }
                }
                timeEnd = DateTime.Now;
                diff = timeEnd - timeStart;
                //Console.WriteLine($"Time span, completion: {diff}");
            }
            if (tableDataType == indexDataType)
            {
                result = dt;
            }
            else
            {
                result = GetParentDataTable(indexDataType);
            }


            return result;
        }

        public async Task LoadCSVFile(ConnectParameters source, ConnectParameters target, string fileName)
        {
            DateTime timeStart = DateTime.Now;

            connectionString = target.ConnectionString;
            string accessJson = await fileStorageService.ReadFile("connectdefinition", "PPDMDataAccess.json");
            _dataDef = JsonConvert.DeserializeObject<List<DataAccessDef>>(accessJson);
            string referenceJson = await fileStorageService.ReadFile("connectdefinition", "PPDMReferenceTables.json");
            _references = JsonConvert.DeserializeObject<List<ReferenceTable>>(referenceJson);
            string csvJson = await fileStorageService.ReadFile("connectdefinition", "CSVDataAccess.json");
            _csvDef = JsonConvert.DeserializeObject<List<CSVAccessDef>>(csvJson);

            //Console.WriteLine("start reading csv file");
            string csvText = await fileStorageService.ReadFile(source.Catalog, fileName);
            DateTime timeEnd = DateTime.Now;
            TimeSpan diff = timeEnd - timeStart;
            //Console.WriteLine($"Time span, read all definitions files: {diff}");

            string dataType = source.DataType.Remove(source.DataType.Length - 1, 1);
            dataAccess = _dataDef.First(x => x.DataType == dataType);

            CSVAccessDef csvAccess = _csvDef.First(x => x.DataType == dataType);
            Dictionary<string, int> attributes = csvAccess.Mappings.ToDictionary();
            Dictionary<string, string> constants = csvAccess.Constants.ToStringDictionary();
            Dictionary<string, string> columnTypes = dt.GetColumnTypes();

            //Console.WriteLine("Start parsing csv file");
            using (TextReader csvStream = new StringReader(csvText))
            {
                var conf = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    //BadDataFound = null
                };
                using (var csv = new CsvReader(csvStream, conf))
                {
                    //csv.Configuration.BadDataFound = null; //Only works with version 19.0.0
                    using (var dr = new CsvDataReader(csv))
                    {
                        dt = new DataTable();
                        dt.Load(dr);
                        timeEnd = DateTime.Now;
                        diff = timeEnd - timeStart;
                        //Console.WriteLine($"Time span, transfer from csv to datatable: {diff}");

                        dt = FixTableColumns(dt, attributes, dataType);
                        timeEnd = DateTime.Now;
                        diff = timeEnd - timeStart;
                        //Console.WriteLine($"Time span, column changes: {diff}");

                        InsertTableToDatabase(attributes, dataType, target, constants);
                    }
                }
            }

            timeEnd = DateTime.Now;
            diff = timeEnd - timeStart;
            //Console.WriteLine($"Time span, completion: {diff}");
        }

        private DataTable GetParentDataTable(string dataType)
        {
            DataTable pt = new DataTable();
            dataAccess = _dataDef.First(x => x.DataType == dataType);
            string select = dataAccess.Select;
            string table = Common.GetTable(select);
            List<ReferenceTable> dataTypeRefs = _references.Where(x => x.DataType == tableDataType).ToList();
            foreach (ReferenceTable referance in dataTypeRefs)
            {
                if (referance.Table == table)
                {
                    string columnName = referance.ReferenceAttribute;
                    List<string> distinctIds = dt.AsEnumerable().Select(row => row.Field<string>(columnName)).Distinct().ToList();
                    pt = NewDataTable(dataType);
                    int keyAttributeLength = GetAttributeLength(referance.Table, referance.KeyAttribute);
                    foreach (string id in distinctIds)
                    {
                        if (!string.IsNullOrEmpty(id))
                        {
                            string newId = id;
                            if (newId.Length > keyAttributeLength)
                            {
                                newId = newId.Substring(0, keyAttributeLength);
                                if (newId.Substring(keyAttributeLength - 1, 1) == "'")
                                {
                                    newId = newId.Substring(0, keyAttributeLength - 1);
                                }

                                DataRow[] sl = dt.Select($"{columnName} = '{id}'");
                                foreach (DataRow r in sl)
                                    r[columnName] = newId;
                            }
                            string tmpId = Common.FixAposInStrings(newId);
                            string key = $"{referance.KeyAttribute} = '{tmpId}'";
                            DataRow[] rows = pt.Select(key);
                            if (rows.Length == 0)
                            {
                                DataRow newRow = pt.NewRow();
                                newRow[referance.KeyAttribute] = newId;
                                newRow[referance.ValueAttribute] = id;
                                if (!string.IsNullOrEmpty(referance.FixedKey))
                                {
                                    string[] fixedKey = referance.FixedKey.Split('=');
                                    newRow[fixedKey[0]] = fixedKey[1];
                                }
                                pt.Rows.Add(newRow);
                            }
                        }
                    }
                }
            }
            return pt;
        }

        private DataTable NewDataTable(string dataType)
        {
            string select = dataAccess.Select;
            string query = $" where 0 = 1";
            DbUtilities db = new DbUtilities();
            db.OpenWithConnectionString(connectionString);
            DataTable table = db.GetDataTable(select, query);
            db.CloseConnection();
            return table;
        }

        private void InitDuplicateKeys(string dataType)
        {
            dataAccess = _dataDef.First(x => x.DataType == dataType);
            string[] keys = dataAccess.Keys.Split(',').Select(key => key.Trim()).ToArray();
            foreach (DataRow row in dt.Rows)
            {
                string duplicateKey = "";
                foreach (string key in keys)
                {
                    string value = row[key].ToString().ToUpper();
                    duplicateKey = duplicateKey + value;
                }
                duplicates.Add(duplicateKey.GetSHA256Hash(), "");
            }
        }

        private int GetAttributeLength(string table, string key)
        {
            int length = -1;
            string select = $"Select * from INFORMATION_SCHEMA.COLUMNS where TABLE_NAME = '{table}'";
            SqlDataAdapter schema = new SqlDataAdapter(select, connectionString);
            DataTable st = new DataTable();
            schema.Fill(st);
            select = $"COLUMN_NAME = '{key}'";
            DataRow[] rows = st.Select(select);
            string attributeType = rows[0]["DATA_TYPE"].ToString();
            if (attributeType == "nvarchar")
            {
                length = (int)rows[0]["CHARACTER_MAXIMUM_LENGTH"];
            }
            return length;
        }

        private string GetDataKey(string[] fields, Dictionary<string, int> attributes)
        {
            string dataKey = "";
            string and = "";
            string[] keys = dataAccess.Keys.Split(',');
            foreach (string key in keys)
            {
                string attribute = key.Trim();
                string value = "";
                if (attributes.ContainsKey(attribute))
                {
                    int column = attributes[attribute];
                    if (string.IsNullOrEmpty(fields[column]))
                    {
                        dataKey = "";
                        return dataKey;
                    }
                    value = fields[column];
                }
                else
                {
                    value = "UNKNOWN";
                }
                string attributeValue = "'" + value + "'";
                dataKey = dataKey + and + attribute + " = " + attributeValue;
                and = " AND ";
            }
            return dataKey;
        }

        private string GetDuplicateKey(string[] fields, Dictionary<string, int> attributes)
        {
            string dataKey = "";
            //string and = "";
            string[] keys = dataAccess.Keys.Split(',');
            foreach (string key in keys)
            {
                string attribute = key.Trim();
                string value = "";
                if (attributes.ContainsKey(attribute))
                {
                    int column = attributes[attribute];
                    if (string.IsNullOrEmpty(fields[column]))
                    {
                        dataKey = "";
                        return dataKey;
                    }
                    value = fields[column].ToUpper();
                }
                else
                {
                    value = "UNKNOWN";
                }
                dataKey = dataKey + value;
            }
            return dataKey.GetSHA256Hash();
        }

        public DataRow InsertCSVRow(DataRow row,
            Dictionary<string, int> attributes,
            string[] fields,
            Dictionary<string, string> constants)
        {
            DataColumnCollection dataColumns = dt.Columns;
            DataRow tmpRow = row;
            foreach (var item in attributes)
            {
                string attribute = item.Key;
                int column = item.Value;
                string attributeValue = fields[column];
                Type columnType = dataColumns.Cast<DataColumn>().Where(c => c.ColumnName == attribute).Select(x => x.DataType).First();
                if (columnType.Name == "Decimal")
                {
                    decimal number;
                    if (decimal.TryParse(attributeValue, out number))
                    {
                        tmpRow[attribute] = number;
                    }
                }
                else if (columnType.Name == "String")
                {
                    if (!string.IsNullOrEmpty(attributeValue))
                    {
                        int length = dataColumns.Cast<DataColumn>().Where(c => c.ColumnName == attribute).Select(x => x.MaxLength).First();
                        if (attributeValue.Length > length) tmpRow[attribute] = attributeValue.Substring(0, length);
                        else tmpRow[attribute] = attributeValue;
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(attributeValue))
                    {
                        tmpRow[attribute] = attributeValue;
                    }
                }
            }

            foreach (var constant in constants)
            {
                string attribute = constant.Key;
                string attributeValue = constant.Value;
                tmpRow[attribute] = attributeValue;
            }

            return tmpRow;
        }

        private void InsertTableToDatabase(Dictionary<string, int> attributes, string dataType,
            ConnectParameters target, Dictionary<string, string> constants)
        {
            int sqlTimeout = 3600;
            string tempTable = "#MyTempTable";
            DateTime timeStart = DateTime.Now;
            SqlConnection conn = new SqlConnection(connectionString);

            string sql = CreateTempTableSql(tempTable);
            SqlCommand cmd = new SqlCommand(sql, conn);
            conn.Open();
            cmd.ExecuteNonQuery();

            SqlBulkCopy bulkCopy = new SqlBulkCopy(conn);
            bulkCopy.DestinationTableName = tempTable + "1";
            bulkCopy.BulkCopyTimeout = 300;
            bulkCopy.WriteToServer(dt);
            DateTime timeEnd = DateTime.Now;
            TimeSpan diff = timeEnd - timeStart;
            //Console.WriteLine($"Time span, Moved to temp table: {diff}");

            sql = CreateSqlToFixConstants(tempTable, constants);
            if (!String.IsNullOrEmpty(sql))
            {
                cmd = new SqlCommand(sql, conn);
                cmd.CommandTimeout = sqlTimeout;
                cmd.ExecuteNonQuery();
            }

            sql = CreateSqlToFixNullKeys(tempTable);
            cmd = new SqlCommand(sql, conn);
            cmd.CommandTimeout = sqlTimeout;
            cmd.ExecuteNonQuery();
            timeEnd = DateTime.Now;
            diff = timeEnd - timeStart;
            //Console.WriteLine($"Time span, delete null rows: {diff}");

            sql = CreateSqlToDeleteDuplicates(tempTable);
            cmd = new SqlCommand(sql, conn);
            cmd.CommandTimeout = sqlTimeout;
            cmd.ExecuteNonQuery();
            timeEnd = DateTime.Now;
            diff = timeEnd - timeStart;
            //Console.WriteLine($"Time span, delete duplicate rows: {diff}");

            int refCount = _references.Where(x => x.DataType == dataType).Count();
            if (refCount > 0) CreateSqlToLoadReferences(dataType, conn, tempTable);

            sql = CreateSqlToMerge(tempTable);
            cmd = new SqlCommand(sql, conn);
            cmd.CommandTimeout = sqlTimeout;
            cmd.ExecuteNonQuery();
            timeEnd = DateTime.Now;
            diff = timeEnd - timeStart;
            //Console.WriteLine($"Time span, merge: {diff}");

            conn.Close();

        }

        private string CreateSqlToFixConstants(string tempTable, Dictionary<string, string> constants)
        {
            string sql = "";
            if (constants.Count > 0)
            {
                sql = $"UPDATE {tempTable}1 SET ";
                string comma = "";
                foreach (var item in constants)
                {
                    string column = item.Key.Trim();
                    string value = item.Value.Trim();
                    sql = sql + comma + column + " = '" + value + "'";
                    comma = ", ";
                }
            }
            return sql;
        }

        private void CreateSqlToLoadReferences(string dataType, SqlConnection conn, string tempTable)
        {
            DbUtilities dbConn = new DbUtilities();
            dbConn.OpenWithConnectionString(connectionString);
            string dataTypeSql = dataAccess.Select;
            ColumnProperties attributeProperties = CommonDbUtilities.GetColumnSchema(dbConn, dataTypeSql);
            dbConn.CloseConnection();

            string sql = "";
            string alterSql = $"ALTER TABLE {tempTable}2 ADD ";
            string updateSql = $"SET ANSI_WARNINGS OFF UPDATE {tempTable}2 SET ";
            string removeSql = $"ALTER TABLE {tempTable}2 DROP COLUMN ";
            string renameSql = " ";
            string nullSql = " ";
            string insertSql = "";
            string addKeySql = $"ALTER TABLE {tempTable}2 ADD ";
            string addKeyValueSql = $"UPDATE {tempTable}2 SET ";
            string insertAdditionalRefTable = "";
            string comma = "";
            string keyComma = "";

            List<ReferenceTable> dataTypeRefs = _references.Where(x => x.DataType == dataType).ToList();
            foreach (ReferenceTable refTable in dataTypeRefs)
            {
                string column = refTable.ReferenceAttribute;
                string dataProperty = attributeProperties[column];
                string valueAttribute = refTable.ValueAttribute;
                if (dt.Columns.Contains(column))
                {
                    alterSql = alterSql + comma + $"{column}2 {dataProperty}";
                    updateSql = updateSql + comma + $"{column}2 = {column}";
                    removeSql = removeSql + comma + $"{column}";
                    renameSql = renameSql + $"EXEC tempdb.sys.sp_rename '{tempTable}2.{column}2', '{column}', 'COLUMN';";
                    //renameSql = renameSql + $"EXEC sp_rename '{tempTable}2.{column}2', '{column}', 'COLUMN';";
                    nullSql = nullSql + $"update {tempTable}2 set {column} = 'UNKNOWN'" +
                        $" where {column} is null or {column} = '';";
                    string insertColumns;
                    string selectColumns;
                    if (valueAttribute == refTable.KeyAttribute)
                    {
                        insertColumns = $"{refTable.KeyAttribute}";
                        selectColumns = $"B.{column}";
                    }
                    else
                    {
                        insertColumns = $"{refTable.KeyAttribute}, {valueAttribute}";
                        selectColumns = $"B.{column}, B.{column}";
                    }
                    if (!string.IsNullOrEmpty(refTable.FixedKey))
                    {
                        string[] fixedKey = refTable.FixedKey.Split('=');
                        insertColumns = insertColumns + ", " + fixedKey[0];
                        selectColumns = selectColumns + ", '" + fixedKey[1] + "'";
                    }
                    insertColumns = insertColumns +
                        ", ROW_CREATED_DATE, ROW_CREATED_BY" +
                        ", ROW_CHANGED_DATE, ROW_CHANGED_BY";
                    selectColumns = selectColumns +
                        ", CAST(GETDATE() AS DATE), @user" +
                        ", CAST(GETDATE() AS DATE), @user";
                    insertSql = insertSql + $"INSERT INTO {refTable.Table} ({insertColumns})" +
                        $" SELECT distinct {selectColumns} from {tempTable}2 B" +
                        $" LEFT JOIN {refTable.Table} A ON A.{refTable.KeyAttribute} = B.{column} WHERE A.{refTable.KeyAttribute} is null;";
                    //$" EXCEPT SELECT {insertColumns} from {refTable.Table};";
                    comma = ",";
                }
            }

            alterSql = alterSql + ";";
            SqlCommand cmd = new SqlCommand(alterSql, conn);
            cmd.ExecuteNonQuery();

            updateSql = updateSql + "; SET ANSI_WARNINGS ON";
            cmd = new SqlCommand(updateSql, conn);
            cmd.ExecuteNonQuery();

            removeSql = removeSql + ";";
            renameSql = renameSql + " ";
            nullSql = nullSql + " ";
            sql = removeSql + renameSql + nullSql;
            cmd = new SqlCommand(sql, conn);
            cmd.ExecuteNonQuery();

            insertSql = "DECLARE @user varchar(30);" +
                @"SET @user = stuff(suser_sname(), 1, charindex('\', suser_sname()), '');" +
                insertSql;
            cmd = new SqlCommand(insertSql, conn);
            cmd.ExecuteNonQuery();
        }

        private string CreateSqlToMerge(string tempTable)
        {
            string sql = "";
            DbUtilities dbConn = new DbUtilities();
            dbConn.OpenWithConnectionString(connectionString);
            string dataTypeSql = dataAccess.Select;
            string table = Common.GetTable(dataTypeSql);
            ColumnProperties attributeProperties = CommonDbUtilities.GetColumnSchema(dbConn, dataTypeSql);
            dbConn.CloseConnection();
            string[] columnNames = dt.Columns.Cast<DataColumn>()
                         .Select(x => x.ColumnName)
                         .ToArray();

            string updateSql = "";
            string comma = "";
            foreach (string colName in columnNames)
            {
                string dataProperty = attributeProperties[colName];
                if (dataProperty.Contains("varchar"))
                {
                    updateSql = updateSql + comma + colName + " = B." + colName;
                }
                else
                {
                    updateSql = updateSql + comma + colName + " = TRY_CAST(B." + colName + " as " + dataProperty + ")";
                }
                comma = ",";
            }
            updateSql = updateSql + ", ROW_CHANGED_DATE = CAST(GETDATE() AS DATE)," +
                "ROW_CHANGED_BY = @user";

            string insertSql = "";
            string valueSql = "";
            comma = "";
            foreach (string colName in columnNames)
            {
                string dataProperty = attributeProperties[colName];
                insertSql = insertSql + comma + colName;
                if (dataProperty.Contains("varchar"))
                {
                    valueSql = valueSql + comma + " B." + colName;
                }
                else
                {
                    valueSql = valueSql + comma + " TRY_CAST(B." + colName + " as " + dataProperty + ")";
                }
                comma = ",";
            }
            insertSql = insertSql + ", ROW_CHANGED_DATE, ROW_CREATED_DATE, ROW_CHANGED_BY, ROW_CREATED_BY";
            valueSql = valueSql + ", CAST(GETDATE() AS DATE), CAST(GETDATE() AS DATE)," +
                "@user, @user";

            string[] keys = dataAccess.Keys.Split(',').Select(k => k.Trim()).ToArray();
            string and = "";
            string joinSql = "";
            foreach (string key in keys)
            {
                joinSql = joinSql + and + "A." + key + " = B." + key;
                and = " AND ";
            }

            sql = "DECLARE @user varchar(30);" +
                @"SET @user = stuff(suser_sname(), 1, charindex('\', suser_sname()), '');" +
                $"MERGE INTO {table} A " +
                $" USING {tempTable}2 B " +
                " ON " + joinSql +
                " WHEN MATCHED THEN " +
                " UPDATE " +
                " SET " + updateSql +
                " WHEN NOT MATCHED THEN " +
                " INSERT(" + insertSql + ") " +
                " VALUES(" + valueSql + "); ";


            return sql;
        }

        private string CreateSqlToDeleteDuplicates(string tempTable)
        {
            string sql = "";
            string[] keys = dataAccess.Keys.Split(',').Select(k => k.Trim()).ToArray();
            sql = $"select * into {tempTable}2 from {tempTable}1 " +
                $"where id in (select max(id) from {tempTable}1 group by ";
            string comma = "";
            foreach (string key in keys)
            {
                if (dt.Columns.Contains(key))
                {
                    sql = sql + comma + key;
                    comma = ",";
                }
            }
            sql = sql + ")";
            return sql;
        }

        private string CreateSqlToDeleteNullRows(string tempTable)
        {
            string sql = "";
            string[] keys = dataAccess.Keys.Split(',').Select(k => k.Trim()).ToArray();
            sql = $"Delete From {tempTable}1 Where(";
            string or = "";
            foreach (string key in keys)
            {
                if (dt.Columns.Contains(key))
                {
                    sql = sql + or + key + " is null or " + key + @" = ''";
                    or = " or ";
                }
            }
            sql = sql + ")";
            return sql;
        }

        private string CreateSqlToFixNullKeys(string tempTable)
        {
            string sql = "";
            string[] keys = dataAccess.Keys.Split(',').Select(k => k.Trim()).ToArray();
            sql = $"";
            foreach (string key in keys)
            {
                sql = sql + $"UPDATE {tempTable}1 SET {key} = 'UNKNOWN' where {key} is null or {key} = '';";
            }
            return sql;
        }

        private string CreateTempTableSql(string tempTable)
        {
            string sql = "";
            string columns = "";
            string comma = "";
            string[] columnNames = dt.Columns.Cast<DataColumn>()
                         .Select(x => x.ColumnName)
                         .ToArray();

            foreach (var cName in columnNames)
            {
                columns = columns + comma + cName + " varchar(100)";
                comma = ",";
            }
            columns = columns + ", id INT IDENTITY(1,1)";

            sql = $"create table {tempTable}1({columns})";
            return sql;
        }

        private DataTable FixTableColumns(DataTable dt, Dictionary<string, int> attributes, string dataType)
        {
            foreach (var item in attributes)
            {
                dt.Columns[item.Value].ColumnName = item.Key;
            }
            string[] columnNames = dt.Columns.Cast<DataColumn>()
                         .Select(x => x.ColumnName)
                         .ToArray();

            foreach (var cName in columnNames)
            {
                if (!attributes.ContainsKey(cName)) dt.Columns.Remove(cName);
            }

            dataAccess = _dataDef.First(x => x.DataType == dataType);
            string[] keys = dataAccess.Keys.Split(',').Select(key => key.Trim()).ToArray();
            foreach (string key in keys)
            {
                if (!dt.Columns.Contains(key))
                {
                    dt.Columns.Add(key);
                }
            }

            return dt;
        }
    }
}
