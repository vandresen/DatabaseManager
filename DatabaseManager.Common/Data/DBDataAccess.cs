using DatabaseManager.Common.DBAccess;
using DatabaseManager.Common.Helpers;
using DatabaseManager.Shared;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Common.Data
{
    public class DBDataAccess : IDataAccess
    {
        private readonly IADODataAccess _db;
        private string databaseConnectionString;

        public DBDataAccess(IADODataAccess db)
        {
            _db = db;
        }

        public void CloseConnection()
        {
        }

        public async Task<DataTable> GetDataTable(string select, string query, string dataType)
        {
            string sql = select + query;
            DataTable dt = _db.GetDataTable(sql, databaseConnectionString);
            return dt;
        }

        public void OpenConnection(ConnectParameters source, ConnectParameters target)
        {
            databaseConnectionString = source.ConnectionString;
        }
    }
}
