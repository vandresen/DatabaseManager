using DatabaseManager.Common.DBAccess;
using DatabaseManager.Common.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Common.Data
{
    public class SystemDBData : ISystemData
    {
        public class ForeignKeyInfo
        {
            public string FkName { get; set; }
            public string SchemaName { get; set; }
            public string Table { get; set; }
            public string ReferencedTable { get; set; }
            public string ReferencedColumn { get; set; }
        }

        private readonly IDapperDataAccess _dp;
        public SystemDBData()
        {

        }

        public SystemDBData(IDapperDataAccess dp)
        {
            _dp = dp;
        }

        public async Task<IEnumerable<ForeignKeyInfo>> GetForeignKeyInfo(string connectionString, string table, string column)
        {
            string sql = "SELECT obj.name AS FkName, sch.name AS[SchemaName], " +
                        "tab1.name AS[Table], col1.name AS[Column], tab2.name AS[ReferencedTable], col2.name AS[ReferencedColumn] " +
                        "FROM sys.foreign_key_columns fkc " +
                        "INNER JOIN sys.objects obj ON obj.object_id = fkc.constraint_object_id " +
                        "INNER JOIN sys.tables tab1 ON tab1.object_id = fkc.parent_object_id " +
                        "INNER JOIN sys.schemas sch ON tab1.schema_id = sch.schema_id " +
                        "INNER JOIN sys.columns col1 ON col1.column_id = parent_column_id AND col1.object_id = tab1.object_id " +
                        "INNER JOIN sys.tables tab2 ON tab2.object_id = fkc.referenced_object_id " +
                        "INNER JOIN sys.columns col2 ON col2.column_id = referenced_column_id AND col2.object_id = tab2.object_id " +
                        $"where tab1.name = '{table}' and col1.name = '{column}'";
            IEnumerable<ForeignKeyInfo> result = await _dp.ReadData<ForeignKeyInfo>(sql, connectionString);
            if (result == null)
            {
                throw new ArgumentException("Table does not exist");
            }

            return result;
        }

        public async Task<IEnumerable<TableSchema>> GetColumnSchema(string connectionString, string table)
        {
            string sql = $"Select COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, NUMERIC_PRECISION, NUMERIC_SCALE" +
                  " from INFORMATION_SCHEMA.COLUMNS " +
                  $" where TABLE_NAME = '{table}'";
            IEnumerable<TableSchema> result = await _dp.ReadData<TableSchema>(sql, connectionString);
            if (result == null)
            {
                throw new ArgumentException("Table does not exist");
            }

            return result;
        }

        public async Task<string> GetUserName(string connectionString)
        {
            string sql = @"select stuff(suser_sname(), 1, charindex('\', suser_sname()), '') as UserName";
            IEnumerable<string> result = await _dp.ReadData<string>(sql, connectionString);
            string userName = result.FirstOrDefault();
            if (userName == null) userName = "UNKNOWN";
            return userName;
        }
    }
}
