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
        private readonly IDapperDataAccess _dp;
        public SystemDBData()
        {

        }

        public SystemDBData(IDapperDataAccess dp)
        {
            _dp = dp;
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
