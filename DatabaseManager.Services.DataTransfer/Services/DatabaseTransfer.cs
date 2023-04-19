using DatabaseManager.Services.DataTransfer.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.DataTransfer.Services
{
    public class DatabaseTransfer : IDataTransfer
    {
        private int _sqlTimeOut = 1000;
        public DatabaseTransfer()
        {

        }

        public void CopyData(TransferParameters transParms)
        {
            throw new NotImplementedException();
        }

        public void DeleteData(ConnectParametersDto source, string table)
        {
            SqlConnection conn = null;
            string storedProcedure = "dbo.spFastDelete";
            string paramName = "@TableName";
            using (conn = new SqlConnection(source.ConnectionString))
            {
                SqlCommand sqlCmd = new SqlCommand(storedProcedure);
                conn.Open();
                sqlCmd.Connection = conn;
                sqlCmd.CommandType = CommandType.StoredProcedure;
                sqlCmd.CommandTimeout = _sqlTimeOut;
                sqlCmd.Parameters.AddWithValue(paramName, table);
                sqlCmd.ExecuteNonQuery();
            }
        }

        public async Task<List<string>> GetContainers(ConnectParametersDto source)
        {
            List<string> containers = new List<string>();
            foreach (string tableName in DatabaseTables.Names)
            {
                containers.Add(tableName);
            }
            return containers;
        }
    }
}
