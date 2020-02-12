using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DatabaseManager.Server.Helpers;
using DatabaseManager.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace DatabaseManager.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CopyController : ControllerBase
    {
        [HttpPost]
        public ActionResult Copy(TransferParameters transferParameters)
        {
            string table = transferParameters.Table;
            try
            {
                CopyTable(transferParameters);
            }
            catch (Exception)
            {
                return BadRequest();
            }

            string message = $"{table} has been copied";
            return Ok(message);
        }

        private void CopyTable(TransferParameters transferParameters)
        {
            ConnectParameters destination = new ConnectParameters();
            destination.Database = transferParameters.TargetDatabase;
            destination.DatabaseServer = transferParameters.TargetDatabaseServer;
            destination.DatabaseUser = transferParameters.TargetDatabaseUser;
            destination.DatabasePassword = transferParameters.TargetDatabasePassword;
            string destCnStr = DbUtilities.GetConnectionString(destination);

            ConnectParameters source = new ConnectParameters();
            source.Database = transferParameters.SourceDatabase;
            source.DatabaseServer = transferParameters.SourceDatabaseServer;
            source.DatabaseUser = transferParameters.SourceDatabaseUser;
            source.DatabasePassword = transferParameters.SourceDatabasePassword;
            string sourceCnStr = DbUtilities.GetConnectionString(source);

            string table = transferParameters.Table;

            SqlConnection sourceConn = new SqlConnection(sourceCnStr);
            SqlConnection destinationConn = new SqlConnection(destCnStr);
            sourceConn.Open();
            destinationConn.Open();

            BulkCopy(sourceConn, destinationConn, table);

            sourceConn.Close();
            destinationConn.Close();
        }

        private void BulkCopy(SqlConnection source, SqlConnection destination, string table)
        {
            string sql = $"select * from {table}";
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
    }
}