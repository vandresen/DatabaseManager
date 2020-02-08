using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DatabaseManager.Server.Helpers;
using DatabaseManager.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DatabaseManager.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DeleteController : ControllerBase
    {
        [HttpPost]
        public ActionResult Delete(TransferParameters transferParameters)
        {
            DbUtilities dbConn = new DbUtilities();
            ConnectParameters destination = new ConnectParameters();
            destination.Database = transferParameters.TargetDatabase;
            destination.DatabaseServer = transferParameters.TargetDatabaseServer;
            destination.DatabaseUser = transferParameters.TargetDatabaseUser;
            destination.DatabasePassword = transferParameters.TargetDatabasePassword;
            string table = transferParameters.Table;
            try
            {
                dbConn.OpenConnection(destination);
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