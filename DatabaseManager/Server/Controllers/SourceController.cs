using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DatabaseManager.Server.Entities;
using DatabaseManager.Server.Helpers;
using DatabaseManager.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Configuration;

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

        public async Task<ActionResult<List<ConnectParameters>>> Get()
        {
            List<ConnectParameters> connectors = new List<ConnectParameters>();
            CloudTable table = Common.GetTableConnect(connectionString, container);

            TableQuery<SourceEntity> tableQuery = new TableQuery<SourceEntity>().
                Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "PPDM"));
            foreach (SourceEntity entity in table.ExecuteQuery(tableQuery))
            {
                connectors.Add(new ConnectParameters()
                {
                    SourceName = entity.RowKey,
                    Database = entity.DatabaseName,
                    DatabaseServer = entity.DatabaseServer,
                    DatabasePassword = entity.Password,
                    ConnectionString = entity.ConnectionString,
                    DatabaseUser = entity.User
                });
            }

            return connectors;
        }

        [HttpPost]
        public async Task<ActionResult<string>> SaveSource(ConnectParameters connectParameters)
        {
            try
            {
                CloudTable table = Common.GetTableConnect(connectionString, container);
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