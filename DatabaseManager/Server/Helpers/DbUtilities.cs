using DatabaseManager.Shared;
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

        public void OpenConnection(ConnectParameters connection)
        {
            string connectionString = GetConnectionString(connection);
            sqlCn = new SqlConnection();
            sqlCn.ConnectionString = connectionString;
            sqlCn.Open();
        }

        public void CloseConnection()
        {
            sqlCn.Close();
        }

        public void DBDelete(string table, string query = "")
        {
            int rows;
            string sql = "Delete from " + table + " " + query;
            using (SqlCommand cmd = new SqlCommand(sql, this.sqlCn))
            {
                try
                {
                    cmd.CommandTimeout = _sqlTimeOut;
                    rows = cmd.ExecuteNonQuery();
                }
                catch (SqlException ex)
                {
                    Exception error = new Exception($"Sorry! Error deleting from table {table}: {ex}");
                    throw error;
                }
            }
        }

        private string GetConnectionString(ConnectParameters connection)
        {
            string source = $"Source={connection.DatabaseServer};";
            string database = $"Initial Catalog ={connection.Database};";
            string timeout = "Connection Timeout=180";
            string persistSecurity = "Persist Security Info=False;";
            string multipleActive = "MultipleActiveResultSets=True;";
            string integratedSecurity = "";
            string user = "";
            //Encryption is currently not used, more testing later
            string encryption = "Encrypt=True;TrustServerCertificate=False;";
            if (!string.IsNullOrWhiteSpace(connection.DatabaseUser))
                user = $"User ID={connection.DatabaseUser};";
            else
                integratedSecurity = "Integrated Security=True;";
            string password = "";
            if (!string.IsNullOrWhiteSpace(connection.DatabasePassword)) password = $"Password={connection.DatabasePassword};";

            string cnStr = "Data " + source + persistSecurity + database +
                user + password + integratedSecurity + multipleActive;

            cnStr = cnStr + timeout;

            return cnStr;
        }
    }
}
