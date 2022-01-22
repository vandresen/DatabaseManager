//using DatabaseManager.Server.Helpers;
using DatabaseManager.Common.Helpers;
using DatabaseManager.Shared;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace DatabaseManager.Server.Services
{
    public class DBDataAccess : IDataAccess
    {
        private DbUtilities dbConn;

        public DBDataAccess()
        {
            dbConn = new DbUtilities();
        }

        public void CloseConnection()
        {
            dbConn.CloseConnection();
        }

        public async Task<DataTable> GetDataTable(string select, string query, string dataType)
        {
            DataTable dt = dbConn.GetDataTable(select, query);
            return dt;
        }

        public void OpenConnection(ConnectParameters source, ConnectParameters target)
        {
            dbConn.OpenConnection(source);
        }
    }
}
