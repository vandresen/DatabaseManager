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
        private readonly ITableStorageService tableStorageService;
        private readonly IFileStorageService fileStorageService;

        public SourceController(IConfiguration configuration,
            ITableStorageService tableStorageService,
            IFileStorageService fileStorageService)
        {
            connectionString = configuration.GetConnectionString("AzureStorageConnection");
            this.tableStorageService = tableStorageService;
            this.fileStorageService = fileStorageService;
        }

        [HttpGet]
        public async Task<ActionResult<List<ConnectParameters>>> Get()
        {
            List<SourceEntity> sourceEntities = new List<SourceEntity>();
            List<ConnectParameters> connectors = new List<ConnectParameters>();
            try
            {
                string tmpConnString = Request.Headers["AzureStorageConnection"];
                tableStorageService.SetConnectionString(tmpConnString);
                fileStorageService.SetConnectionString(tmpConnString);
                string dataAccessDef =  await fileStorageService.ReadFile("connectdefinition", "PPDMDataAccess.json");
                sourceEntities = await tableStorageService.GetTableRecords<SourceEntity>(container);
                foreach (SourceEntity entity in sourceEntities)
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
            catch (Exception ex)
            {
                return NotFound(ex.ToString());
            }
            return connectors;
        }

        [HttpGet("{name}")]
        public async Task<ActionResult<ConnectParameters>> Get(string name)
        {
            ConnectParameters connector = new ConnectParameters();
            SourceEntity entity = new SourceEntity();
            try
            {
                string tmpConnString = Request.Headers["AzureStorageConnection"];
                tableStorageService.SetConnectionString(tmpConnString);
                fileStorageService.SetConnectionString(tmpConnString);
                string dataAccessDef = await fileStorageService.ReadFile("connectdefinition", "PPDMDataAccess.json");
                entity = await tableStorageService.GetTableRecord<SourceEntity>(container, name);
                connector.SourceName = entity.RowKey;
                connector.Database = entity.DatabaseName;
                connector.DatabaseServer = entity.DatabaseServer;
                connector.DatabasePassword = entity.Password;
                connector.ConnectionString = entity.ConnectionString;
                connector.DatabaseUser = entity.User;
                connector.DataAccessDefinition = dataAccessDef;
            }
            catch (Exception ex)
            {
                return NotFound(ex.ToString());
            }
            
            return connector;
        }

        [HttpPut]
        public async Task<ActionResult<string>> UpdateSource(ConnectParameters connectParameters)
        {
            try
            {
                string tmpConnString = Request.Headers["AzureStorageConnection"];
                tableStorageService.SetConnectionString(tmpConnString);
                await tableStorageService.UpdateTable(container, connectParameters);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.ToString());
            }

            return Ok($"OK");
        }

        [HttpPost]
        public async Task<ActionResult<string>> SaveSource(ConnectParameters connectParameters)
        {
            try
            {
                SourceEntity sourceEntity = new SourceEntity(connectParameters.SourceName)
                {
                    DatabaseName = connectParameters.Database,
                    DatabaseServer = connectParameters.DatabaseServer,
                    User = connectParameters.DatabaseUser,
                    Password = connectParameters.DatabasePassword,
                    ConnectionString = connectParameters.ConnectionString
                };
                string tmpConnString = Request.Headers["AzureStorageConnection"];
                tableStorageService.SetConnectionString(tmpConnString);
                await tableStorageService.SaveTableRecord(container, connectParameters.SourceName, sourceEntity);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.ToString());
            }
            
            return Ok($"OK");
        }

        [HttpDelete("{name}")]
        public async Task<ActionResult> Delete(string name)
        {
            try
            {
                string tmpConnString = Request.Headers["AzureStorageConnection"];
                tableStorageService.SetConnectionString(tmpConnString);
                await tableStorageService.DeleteTable(container, name);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.ToString());
            }
            
            return NoContent();
        }
    }
}