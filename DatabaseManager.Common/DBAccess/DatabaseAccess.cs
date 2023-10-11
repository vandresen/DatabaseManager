using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Common.DBAccess
{
    public class DatabaseAccess : IDatabaseAccess
    {
        public DatabaseAccess()
        {
        }
        public void WakeUpDatabase(string connectionString)
        {
            SqlConnection conn = null;
            using (conn = new SqlConnection(connectionString))
            {
                conn.Open();
                conn.Close();
            }
        }
    }
}
