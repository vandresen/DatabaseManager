using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DatabaseManager.Server.Helpers;
using DatabaseManager.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DatabaseManager.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CopyController : ControllerBase
    {
        private string connectionString;
        private readonly string container = "sources";
        private readonly ILogger<CopyController> logger;

        public CopyController(IConfiguration configuration, ILogger<CopyController> logger)
        {
            connectionString = configuration.GetConnectionString("AzureStorageConnection");
            this.logger = logger;
        }

        [HttpPost]
        public ActionResult Copy(TransferParameters transferParameters)
        {
            string tmpConnString = Request.Headers["AzureStorageConnection"];
            if (!string.IsNullOrEmpty(tmpConnString)) connectionString = tmpConnString;
            if (string.IsNullOrEmpty(connectionString)) return NotFound("Connection string is not set");

            string message = "";
            string table = transferParameters.Table;
            try
            {
                CopyTable(transferParameters);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.ToString());
            }

            message = $"{table} has been copied";
            return Ok(message);
        }

        private void CopyTable(TransferParameters transferParameters)
        {
            ConnectParameters destination = Common.GetConnectParameters(connectionString, container,
                transferParameters.TargetName);
            string destCnStr = destination.ConnectionString;

            ConnectParameters source = Common.GetConnectParameters(connectionString, container,
                transferParameters.SourceName);
            string sourceCnStr = source.ConnectionString;

            string table = transferParameters.Table;
            SqlConnection sourceConn = new SqlConnection(sourceCnStr);
            SqlConnection destinationConn = new SqlConnection(destCnStr);
            try
            {   
                sourceConn.Open();
                destinationConn.Open();
                BulkCopy(sourceConn, destinationConn, table);
                
            }
            catch (Exception ex)
            {
                Exception error = new Exception($"Sorry! Error copying table: {table}; {ex}");
                throw error;
            }
            finally
            {
                sourceConn.Close();
                destinationConn.Close();
            }
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