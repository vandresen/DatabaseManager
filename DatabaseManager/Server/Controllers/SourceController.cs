using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DatabaseManager.Server.Entities;
using DatabaseManager.Server.Helpers;
using DatabaseManager.Server.Services;
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
        private string connectionString;
        private readonly string container = "sources";
        private readonly IFileStorageService fileStorageService;

        public SourceController(IConfiguration configuration, IFileStorageService fileStorageService)
        {
            connectionString = configuration.GetConnectionString("AzureStorageConnection");
            this.fileStorageService = fileStorageService;
        }

        [HttpGet]
        public async Task<ActionResult<List<ConnectParameters>>> Get()
        {
            string tmpConnString = Request.Headers["AzureStorageConnection"];
            if (!string.IsNullOrEmpty(tmpConnString)) connectionString = tmpConnString;
            if (string.IsNullOrEmpty(connectionString)) return NotFound("Connection string is not set");
            fileStorageService.SetConnectionString(tmpConnString);

            List<ConnectParameters> connectors = new List<ConnectParameters>();
            try
            {
                string dataAccessDef =  await fileStorageService.ReadFile("connectdefinition", "PPDMDataAccess.json");
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
                        DatabaseUser = entity.User,
                        DataAccessDefinition = dataAccessDef
                    });
                }
            }
            catch (Exception)
            {
                return NotFound();
            }
            
            return connectors;
        }

        [HttpGet("{name}")]
        public async Task<ActionResult<ConnectParameters>> Get(string name)
        {
            string tmpConnString = Request.Headers["AzureStorageConnection"];
            if (!string.IsNullOrEmpty(tmpConnString)) connectionString = tmpConnString;
            if (string.IsNullOrEmpty(connectionString)) return NotFound("Connection string is not set");
            ConnectParameters connector = new ConnectParameters();
            try
            {
                CloudTable table = Common.GetTableConnect(connectionString, container);
                TableOperation retrieveOperation = TableOperation.Retrieve<SourceEntity>("PPDM", name);
                TableResult result = await table.ExecuteAsync(retrieveOperation);
                SourceEntity entity = result.Result as SourceEntity;
                if (entity == null) { return NotFound(); }
                connector.SourceName = name;
                connector.Database = entity.DatabaseName;
                connector.DatabaseServer = entity.DatabaseServer;
                connector.DatabaseUser = entity.User;
                connector.DatabasePassword = entity.Password;
                connector.ConnectionString = entity.ConnectionString;
            }
            catch (Exception)
            {
                return NotFound();
            }
            
            return connector;
        }

        [HttpPut]
        public async Task<ActionResult<string>> UpdateSource(ConnectParameters connectParameters)
        {
            string tmpConnString = Request.Headers["AzureStorageConnection"];
            if (!string.IsNullOrEmpty(tmpConnString)) connectionString = tmpConnString;
            if (string.IsNullOrEmpty(connectionString)) return NotFound("Connection string is not set");
            if (connectParameters == null) return BadRequest();
            try
            {
                CloudTable table = Common.GetTableConnect(connectionString, container);
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
                TableOperation insertOrMergeOperation = TableOperation.InsertOrMerge(sourceEntity);
                TableResult result = await table.ExecuteAsync(insertOrMergeOperation);
            }
            catch (Exception)
            {
                return BadRequest();
            }

            return Ok($"OK");
        }

        [HttpPost]
        public async Task<ActionResult<string>> SaveSource(ConnectParameters connectParameters)
        {
            string tmpConnString = Request.Headers["AzureStorageConnection"];
            if (!string.IsNullOrEmpty(tmpConnString)) connectionString = tmpConnString;
            if (string.IsNullOrEmpty(connectionString)) return NotFound("Connection string is not set");
            if (connectParameters == null) return BadRequest();
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

        [HttpDelete("{name}")]
        public async Task<ActionResult> Delete(string name)
        {
            string tmpConnString = Request.Headers["AzureStorageConnection"];
            if (!string.IsNullOrEmpty(tmpConnString)) connectionString = tmpConnString;
            if (string.IsNullOrEmpty(connectionString)) return NotFound("Connection string is not set");
            try
            {
                CloudTable table = Common.GetTableConnect(connectionString, container);
                TableOperation retrieveOperation = TableOperation.Retrieve<SourceEntity>("PPDM", name);
                TableResult result = await table.ExecuteAsync(retrieveOperation);
                SourceEntity entity = result.Result as SourceEntity;
                if (entity == null)
                {
                    return BadRequest();
                }

                TableOperation deleteOperation = TableOperation.Delete(entity);
                result = await table.ExecuteAsync(deleteOperation);
            }
            catch (Exception)
            {
                return BadRequest();
            }
            
            return NoContent();
        }
    }
}