using DatabaseManager.Services.DataTransfer.Extensions;
using DatabaseManager.Services.DataTransfer.Models;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
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
        private ConnectParametersDto _targetConnector;
        private TransferParameters _transferParameters;
        private string _referenceJson;

        public DatabaseTransfer()
        {

        }

        public void CopyData(TransferParameters transferParameters, ConnectParametersDto sourceConnector, ConnectParametersDto targetConnector, string referenceJson)
        {
            _targetConnector = targetConnector;
            _referenceJson = referenceJson;
            _transferParameters= transferParameters;
            SqlConnection sourceConn = null;
            SqlConnection targetConn = null;
            using (sourceConn = new SqlConnection(sourceConnector.ConnectionString))
            {
                using (targetConn = new SqlConnection(targetConnector.ConnectionString)) 
                {
                    sourceConn.Open();
                    targetConn.Open();
                    BulkCopy(sourceConn, targetConn, transferParameters);
                }
            }
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

        private void BulkCopy(SqlConnection source, SqlConnection destination, TransferParameters transferParameters)
        {
            string sql = "";
            string table = transferParameters.Table;
            string query = "";
            if (CopyTables.dictionary[table] == "TABLE") query = transferParameters.TransferQuery;
            if (string.IsNullOrEmpty(query))
            {
                sql = $"select * from {table}";
            }
            else
            {
                CreateTempTable(source);
                InsertQueryData(source, transferParameters);
                sql = $"select * from {table} where UWI in (select UWI from #PDOList)";
            }

            ProcessReferenceTables(table, source, destination);

            using (SqlCommand cmd = new SqlCommand(sql, source))
            {
                try
                {
                    cmd.CommandTimeout = 3600;
                    SqlDataReader reader = cmd.ExecuteReader();
                    SqlBulkCopy bulkData = new SqlBulkCopy(destination);
                    bulkData.DestinationTableName = table;
                    bulkData.BulkCopyTimeout = 1000;
                    bulkData.WriteToServer(reader);
                    bulkData.Close();
                }
                catch (SqlException ex)
                {
                    Exception error = new Exception($"Sorry! Error copying table: {table}; {ex}");
                    throw error;
                }
            }
        }

        private void ProcessReferenceTables(string table, SqlConnection source, SqlConnection destination)
        {
            string dataType = "";
            List<DataAccessDef> accessDefList = JsonConvert.DeserializeObject<List<DataAccessDef>>(_targetConnector.DataAccessDefinition);
            foreach (DataAccessDef accessDef in accessDefList)
            {
                string selectTable = accessDef.Select.GetTable();
                if (selectTable == table)
                {
                    dataType = accessDef.DataType;
                    break;
                }
            }

            if (!string.IsNullOrEmpty(dataType))
            {
                List<ReferenceTable> allReferences = JsonConvert.DeserializeObject<List<ReferenceTable>>(_referenceJson);
                List<ReferenceTable> references = allReferences.FindAll(x => x.DataType == dataType);
                foreach (var reference in references)
                {
                    if (!DatabaseTables.Names.Contains(reference.Table))
                    {
                        BuildRefTable(reference, table, source, destination);
                    }
                }
            }
        }

        private void BuildRefTable(ReferenceTable reference, string table, SqlConnection source, SqlConnection destination)
        {
            DeleteData(_targetConnector, reference.Table);
            CreateRefTempTable(source, table, reference.ReferenceAttribute);
            string sqlCommand = $" SELECT A.* FROM {reference.Table} A INNER JOIN #REFID B ON A.{reference.KeyAttribute} = B.{reference.ReferenceAttribute}";
            using (SqlCommand cmd = new SqlCommand(sqlCommand, source))
            {
                try
                {
                    cmd.CommandTimeout = 3600;
                    SqlDataReader reader = cmd.ExecuteReader();
                    SqlBulkCopy bulkData = new SqlBulkCopy(destination);
                    bulkData.DestinationTableName = reference.Table;
                    bulkData.BulkCopyTimeout = 1000;
                    bulkData.WriteToServer(reader);
                }
                catch (SqlException ex)
                {
                    Exception error = new Exception($"Sorry! Error copying table: {table}; {ex}");
                    throw error;
                }
            }
        }

        private void CreateRefTempTable(SqlConnection source, string table, string referenceAttribute)
        {
            string sqlCommand = "DROP TABLE IF EXISTS #REFID";
            sqlCommand = sqlCommand + $" SELECT DISTINCT({referenceAttribute}) into #REFID from {table}";
            if (!string.IsNullOrEmpty(_transferParameters.TransferQuery))
            {
                sqlCommand = sqlCommand + $" where UWI in (select UWI from #PDOList)";
            }
            using (SqlCommand cmd = new SqlCommand(sqlCommand, source))
            {
                try
                {
                    cmd.CommandTimeout = 3000;
                    cmd.ExecuteNonQuery();
                }
                catch (SqlException ex)
                {
                    Exception error = new Exception("Error inserting into ref temp table: ", ex);
                    throw error;
                }

            }
        }

        private void CreateTempTable(SqlConnection source)
        {
            string sql = "create table #PDOList (UWI NVARCHAR(40))";
            using (SqlCommand cmd = new SqlCommand(sql, source))
            {
                try
                {
                    cmd.CommandTimeout = 3000;
                    cmd.ExecuteNonQuery();
                }
                catch (SqlException ex)
                {
                    Exception error = new Exception("Error inserting into table: ", ex);
                    throw error;
                }

            }
        }

        private void InsertQueryData(SqlConnection source, TransferParameters transferParameters)
        {
            string query = transferParameters.TransferQuery;
            string queryType = transferParameters.QueryType;
            string sql;
            if (queryType == "File")
            {
                sql = $"DECLARE @Array NVARCHAR(MAX) = '{query}' " +
                    "INSERT INTO #PDOList ( UWI ) " +
                    "SELECT * FROM STRING_SPLIT(@Array, ',')";
            }
            else
            {
                sql = $"INSERT INTO #PDOList (UWI) SELECT UWI FROM WELL {query} ";
            }

            using (SqlCommand cmd = new SqlCommand(sql, source))
            {
                try
                {
                    cmd.CommandTimeout = 3000;
                    cmd.ExecuteNonQuery();
                }
                catch (SqlException ex)
                {
                    Exception error = new Exception("Error inserting into table: ", ex);
                    throw error;
                }

            }
        }
    }
}
