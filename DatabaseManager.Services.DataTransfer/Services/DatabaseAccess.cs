using Dapper;
using DatabaseManager.Services.DataTransfer.Extensions;
using DatabaseManager.Services.DataTransfer.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.DataTransfer.Services
{
    public class DatabaseAccess : IDatabaseAccess
    {
        private int _sqlTimeOut;

        public DatabaseAccess()
        {
            _sqlTimeOut = 1000;
        }

        public async Task<IEnumerable<TableSchema>> GetColumnInfo(string connectionString, string table)
        {
            return await LoadData<TableSchema, dynamic>("dbo.sp_columns", new { TABLE_NAME = table }, connectionString);
        }

        public async Task<string> GetUserName(string connectionString)
        {
            string sql = @"select stuff(suser_sname(), 1, charindex('\', suser_sname()), '') as UserName";
            IEnumerable<string> result = await ReadData<string>(sql, connectionString);
            string userName = result.FirstOrDefault();
            if (userName == null) userName = "UNKNOWN";
            return userName;
        }

        public async Task<IEnumerable<T>> ReadData<T>(string sql, string connectionString)
        {
            using IDbConnection cnn = new SqlConnection(connectionString);
            return await cnn.QueryAsync<T>(sql);
        }

        public async Task<IEnumerable<T>> LoadData<T, U>(string storedProcedure, U parameters, string connectionString)
        {
            using IDbConnection cnn = new SqlConnection(connectionString);
            return await cnn.QueryAsync<T>(storedProcedure, parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task<int> GetCount(string connectionString, string query)
        {
            using IDbConnection cnn = new SqlConnection(connectionString);
            return await cnn.QuerySingleOrDefaultAsync<int>(query, new { });
        }

        public async Task InsertData(string connectionString, string sql)
        {
            using IDbConnection cnn = new SqlConnection(connectionString);
            await cnn.ExecuteAsync(sql, new { });
        }

        public async Task SaveData<T>(string storedProcedure, T parameters, string connectionString)
        {
            using IDbConnection cnn = new SqlConnection(connectionString);
            await cnn.ExecuteAsync(storedProcedure, parameters, commandType: CommandType.StoredProcedure);
        }

        public DataTable GetDataTable(string connectionString, string sql)
        {
            SqlConnection conn = null;
            DataTable result = new DataTable();
            using (conn = new SqlConnection(connectionString))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand(sql, conn);
                SqlDataReader dr = cmd.ExecuteReader();
                result.Load(dr);
                dr.Close();
            }
            return result;
        }

        public void ExecuteSQL(string sql, string connectionString)
        {
            SqlConnection conn = null;
            using (conn = new SqlConnection(connectionString))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.CommandTimeout = _sqlTimeOut;
                cmd.ExecuteNonQuery();
                conn.Close();
            }
        }

        public async Task InsertDataTableToDatabase(string connectionString, DataTable dt, List<ReferenceTable> referenceTables,
            DataAccessDef accessDef)
        {
            _sqlTimeOut = 3600;
            string tempTable = "#MyTempTable";
            string table = accessDef.Select.GetTable();
            IEnumerable<TableSchema> attributeProperties = await GetColumnInfo(connectionString, table);
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string sql = CreateTempTableSql(tempTable, dt);
                SqlCommand cmd = new SqlCommand(sql, conn);
                conn.Open();
                cmd.ExecuteNonQuery();

                SqlBulkCopy bulkCopy = new SqlBulkCopy(conn);
                bulkCopy.DestinationTableName = tempTable + "1";
                bulkCopy.BulkCopyTimeout = 300;
                bulkCopy.WriteToServer(dt);

                if (referenceTables.Count > 0) CreateSqlToLoadReferences(referenceTables, dt, conn, tempTable);

                sql = CreateSqlToMerge(tempTable, accessDef, dt, attributeProperties);
                cmd = new SqlCommand(sql, conn);
                cmd.CommandTimeout = _sqlTimeOut;
                cmd.ExecuteNonQuery();

                conn.Close();
            }
        }

        private string CreateTempTableSql(string tempTable, DataTable dt)
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

        private void CreateSqlToLoadReferences(List<ReferenceTable> referenceTables, DataTable dt,
            SqlConnection conn, string tempTable)
        {
            string insertSql = "";
            string comma = "";
            foreach (ReferenceTable refTable in referenceTables)
            {
                string column = refTable.ReferenceAttribute;
                string valueAttribute = refTable.ValueAttribute;
                if (dt.Columns.Contains(column))
                {
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
                        $" SELECT distinct {selectColumns} from {tempTable}1 B" +
                        $" LEFT JOIN {refTable.Table} A ON A.{refTable.KeyAttribute} = B.{column} WHERE A.{refTable.KeyAttribute} is null;";
                    comma = ",";
                }
            }

            insertSql = "DECLARE @user varchar(30);" +
                @"SET @user = stuff(suser_sname(), 1, charindex('\', suser_sname()), '');" +
                insertSql;
            SqlCommand cmd = new SqlCommand(insertSql, conn);
            cmd.ExecuteNonQuery();
        }

        private string CreateSqlToMerge(string tempTable, DataAccessDef dataAccess, DataTable dt,
            IEnumerable<TableSchema> attributeProperties)
        {
            string sql = "";
            string dataTypeSql = dataAccess.Select;
            string table = dataTypeSql.GetTable();
            string[] columnNames = dt.Columns.Cast<DataColumn>()
                         .Select(x => x.ColumnName)
                         .ToArray();

            string updateSql = "";
            string comma = "";
            foreach (string colName in columnNames)
            {
                TableSchema dataProperty = attributeProperties.FirstOrDefault(x => x.COLUMN_NAME == colName.Trim());
                if (dataProperty.DATA_TYPE.Contains("varchar"))
                {
                    updateSql = updateSql + comma + colName + " = B." + colName;
                }
                else
                {
                    updateSql = updateSql + comma + colName + " = TRY_CAST(B." + colName + " as " + dataProperty.GetDatabaseAttributeType() + ")";
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
                TableSchema dataProperty = attributeProperties.FirstOrDefault(x => x.COLUMN_NAME == colName.Trim());
                insertSql = insertSql + comma + colName;
                if (dataProperty.DATA_TYPE.Contains("varchar"))
                {
                    valueSql = valueSql + comma + " B." + colName;
                }
                else
                {
                    valueSql = valueSql + comma + " TRY_CAST(B." + colName + " as " + dataProperty.GetDatabaseAttributeType() + ")";
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
                $" USING {tempTable}1 B " +
                " ON " + joinSql +
                " WHEN MATCHED THEN " +
                " UPDATE " +
                " SET " + updateSql +
                " WHEN NOT MATCHED THEN " +
                " INSERT(" + insertSql + ") " +
                " VALUES(" + valueSql + "); ";


            return sql;
        }

    }
}
