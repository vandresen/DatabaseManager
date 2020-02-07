using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DatabaseManager.Server.Helpers
{
    public class DbUtilities
    {
        private SqlConnection sqlCn = null;
        private int _sqlTimeOut;

        public DbUtilities()
        {
            _sqlTimeOut = 1000;
        }

        public void OpenConnection(string connectionString)
        {
            sqlCn = new SqlConnection();
            sqlCn.ConnectionString = connectionString;
            sqlCn.Open();
        }

        public void CloseConnection()
        {
            sqlCn.Close();
        }
    }
}
