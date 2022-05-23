﻿using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Common.DBAccess
{
    public class ADODataAccess: IADODataAccess
    {
        private readonly string _connectionString;

        public ADODataAccess()
        {
        }

        public async Task InsertWithUDT<T>(string storedProcedure, string parameterName, T collection, string connectionString)
        {
            SqlConnection conn = null;
            SqlParameter param = new SqlParameter();
            param.ParameterName = parameterName;
            param.SqlDbType = SqlDbType.Structured;
            param.Value = collection;
            param.Direction = ParameterDirection.Input;

            using (conn = new SqlConnection(connectionString))
            {
                SqlCommand sqlCmd = new SqlCommand(storedProcedure);
                conn.Open();
                sqlCmd.Connection = conn;
                sqlCmd.CommandType = CommandType.StoredProcedure;
                sqlCmd.Parameters.Add(param);
                sqlCmd.ExecuteNonQuery();
            }
        }
    }
}
