using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DatabaseManager.Server.Helpers;
using DatabaseManager.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace DatabaseManager.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DeleteController : ControllerBase
    {
        private readonly string connectionString;
        private readonly string container = "sources";

        public DeleteController(IConfiguration configuration)
        {
            connectionString = configuration.GetConnectionString("AzureStorageConnection");
        }

        [HttpPost]
        public ActionResult Delete(TransferParameters transferParameters)
        {
            ConnectParameters connector = Common.GetConnectParameters(connectionString, container, 
                transferParameters.TargetName);
            DbUtilities dbConn = new DbUtilities();
            string table = transferParameters.Table;
            try
            {
                dbConn.OpenConnection(connector);
                if (String.IsNullOrEmpty(table)) return BadRequest();
                dbConn.DBDelete(table);
            }
            catch (Exception)
            {
                return BadRequest();
            }

            string message = $"{table} has been cleared";
            dbConn.CloseConnection();
            return Ok(message);
        }
    }
}