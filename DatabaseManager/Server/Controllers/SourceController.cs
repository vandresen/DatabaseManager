using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using DatabaseManager.Server.Entities;
using DatabaseManager.Server.Services;
using DatabaseManager.Shared;
using Microsoft.AspNetCore.Mvc;
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
        private readonly IMapper mapper;

        public SourceController(IConfiguration configuration,
            ITableStorageService tableStorageService,
            IMapper mapper,
            IFileStorageService fileStorageService)
        {
            connectionString = configuration.GetConnectionString("AzureStorageConnection");
            this.tableStorageService = tableStorageService;
            this.fileStorageService = fileStorageService;
            this.mapper = mapper;
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
                connectors =  mapper.Map<List<ConnectParameters>>(sourceEntities);
                foreach (ConnectParameters connector in connectors)
                {
                    connector.DataAccessDefinition = dataAccessDef;
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
            try
            {
                string tmpConnString = Request.Headers["AzureStorageConnection"];
                tableStorageService.SetConnectionString(tmpConnString);
                fileStorageService.SetConnectionString(tmpConnString);
                string dataAccessDef = await fileStorageService.ReadFile("connectdefinition", "PPDMDataAccess.json");
                SourceEntity entity = await tableStorageService.GetTableRecord<SourceEntity>(container, name);
                ConnectParameters connector = mapper.Map<ConnectParameters>(entity);
                connector.DataAccessDefinition = dataAccessDef;
                return connector;
            }
            catch (Exception ex)
            {
                return NotFound(ex.ToString());
            }
        }

        [HttpPut]
        public async Task<ActionResult<string>> UpdateSource(ConnectParameters connectParameters)
        {
            try
            {
                SourceEntity sourceEntity = mapper.Map<SourceEntity>(connectParameters);
                string tmpConnString = Request.Headers["AzureStorageConnection"];
                tableStorageService.SetConnectionString(tmpConnString);
                await tableStorageService.UpdateTable(container, sourceEntity);
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
                SourceEntity sourceEntity = mapper.Map<SourceEntity>(connectParameters);
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