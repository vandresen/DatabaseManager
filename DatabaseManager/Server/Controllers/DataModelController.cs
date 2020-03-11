using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using DatabaseManager.Server.Helpers;
using DatabaseManager.Shared;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.File;

namespace DatabaseManager.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DataModelController : ControllerBase
    {
        private readonly string connectionString;
        private readonly string container = "sources";

        public DataModelController(IConfiguration configuration)
        {
            connectionString = configuration.GetConnectionString("AzureStorageConnection");
        }

        [HttpPost]
        public async Task<ActionResult<string>> LoadSqlFile(DataModelParameters dmParameters)
        {
            if (dmParameters == null) return BadRequest();
            try
            {
                CloudStorageAccount account = CloudStorageAccount.Parse(connectionString);
                CloudFileClient fileClient = account.CreateCloudFileClient();
                CloudFileShare share = fileClient.GetShareReference(dmParameters.FileShare);
                if (share.Exists())
                {
                    CloudFileDirectory rootDir = share.GetRootDirectoryReference();
                    CloudFile file = rootDir.GetFileReference(dmParameters.FileName);
                    if (file.Exists())
                    {
                        string sql = file.DownloadTextAsync().Result;
                        ConnectParameters connector = Common.GetConnectParameters(connectionString, container, dmParameters.DataConnector);
                        if (connector == null) return BadRequest();
                        DbUtilities dbConn = new DbUtilities();
                        dbConn.OpenConnection(connector);
                        dbConn.SQLExecute(sql);
                        dbConn.CloseConnection();
                    }
                }
                
            }
            catch (Exception ex)
            {
                return BadRequest();
            }

            return Ok($"OK");
        }
    }
}