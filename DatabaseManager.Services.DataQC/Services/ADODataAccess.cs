using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.DataQC.Services
{
    public class ADODataAccess : IDataAccess
    {
        public DataTable GetDataTable(string sql, string connectionString)
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
    }
}
