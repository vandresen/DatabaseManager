using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DatabaseManager.Server.Entities;
using DatabaseManager.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace DatabaseManager.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SourceController : ControllerBase
    {
        private readonly string connectionString;
        private readonly string container = "sources";

        public SourceController(IConfiguration configuration)
        {
            connectionString = configuration.GetConnectionString("AzureStorageConnection");
        }

        [HttpPost]
        public async Task<ActionResult<string>> SaveSource(ConnectParameters connectParameters)
        {
            try
            {
                CloudStorageAccount account = CloudStorageAccount.Parse(connectionString);
                CloudTableClient client = account.CreateCloudTableClient();
                CloudTable table = client.GetTableReference(container);
                await table.CreateIfNotExistsAsync();

                string name = connectParameters.SourceName;
                if (String.IsNullOrEmpty(name)) return BadRequest();
                SourceEntity sourceEntity = new SourceEntity(name)
                {
                    DatabaseName = connectParameters.Database,
                    DatabaseServer = connectParameters.DatabaseServer,
                    User = connectParameters.DatabaseUser,
                    Password = connectParameters.DatabasePassword,
                    ConnectionString = connectParameters.ConnectionString
                };
                TableOperation insertOperation = TableOperation.Insert(sourceEntity);
                await table.ExecuteAsync(insertOperation);
            }
            catch (Exception ex)
            {
                return BadRequest();
            }
            
            return Ok($"OK");
        }
    }
}