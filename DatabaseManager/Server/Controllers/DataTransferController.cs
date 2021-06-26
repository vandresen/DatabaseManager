using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DatabaseManager.Components.Services;
using DatabaseManager.Server.Entities;
using DatabaseManager.Server.Helpers;
using DatabaseManager.Server.Services;
using DatabaseManager.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace DatabaseManager.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DataTransferController : ControllerBase
    {
        private readonly ITableStorageService tableStorageService;
        private readonly IFileStorageService fileStorageService;
        private readonly IQueueService queueService;
        private readonly IMapper mapper;
        private readonly string container = "sources";
        private readonly string queueName = "datatransferqueue";
        private readonly string infoName = "datatransferinfo";

        public DataTransferController(ITableStorageService tableStorageService,
            IFileStorageService fileStorageService,
            IQueueService queueService,
            IMapper mapper)
        {
            this.tableStorageService = tableStorageService;
            this.fileStorageService = fileStorageService;
            this.queueService = queueService;
            this.mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<List<string>>> GetQueueMessage()
        {
            try
            {
                List<string> messages = new List<string>();
                string message = "";
                bool messageBox = true;
                string tmpConnString = Request.Headers["AzureStorageConnection"];
                queueService.SetConnectionString(tmpConnString);
                while (messageBox)
                {
                    message = queueService.GetMessage(infoName);
                    if (string.IsNullOrEmpty(message))
                    {
                        messageBox = false;
                    }
                    else
                    {
                        messages.Add(message);
                    }
                }
                return messages;
            }
            catch (Exception ex)
            {
                return BadRequest("Problems getting info from info queue");
            }
        }

        [HttpGet("{source}")]
        public async Task<ActionResult<List<string>>> Get(string source)
        {
            try
            {
                string tmpConnString = Request.Headers["AzureStorageConnection"];
                fileStorageService.SetConnectionString(tmpConnString);
                tableStorageService.SetConnectionString(tmpConnString);
                SourceEntity entity = await tableStorageService.GetTableRecord<SourceEntity>(container, source);
                List<string> files = new List<string>();
                if (entity.SourceType == "DataBase")
                {
                    foreach (string tableName in DatabaseTables.Names)
                    {
                        files.Add(tableName);
                    }
                }
                else if(entity.SourceType == "File")
                {
                    if (entity.DataType == "Logs")
                    {
                        files = await fileStorageService.ListFiles(entity.Catalog);
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(entity.FileName))
                        {
                            return BadRequest();
                        }
                        files.Add(entity.FileName);
                    }
                }
                
                return files;
            }
            catch (Exception)
            {
                return NotFound();
            }
        }

        [HttpPost]
        public async Task<ActionResult<string>> CopyData(TransferParameters transferParameters)
        {
            try
            {
                string tmpConnString = Request.Headers["AzureStorageConnection"];
                fileStorageService.SetConnectionString(tmpConnString);
                tableStorageService.SetConnectionString(tmpConnString);
                string referenceJson = await fileStorageService.ReadFile("connectdefinition", "PPDMReferenceTables.json");
                string dataAccessDefinition = await fileStorageService.ReadFile("connectdefinition", "PPDMDataAccess.json");
                SourceEntity entity = await tableStorageService.GetTableRecord<SourceEntity>(container, transferParameters.SourceName);
                ConnectParameters sourceConnector = mapper.Map<ConnectParameters>(entity);
                entity = await tableStorageService.GetTableRecord<SourceEntity>(container, transferParameters.TargetName);
                ConnectParameters targetConnector = mapper.Map<ConnectParameters>(entity);
                targetConnector.DataAccessDefinition = dataAccessDefinition;

                if (sourceConnector.SourceType == "DataBase")
                {
                    DatabaseLoader dl = new DatabaseLoader();
                    dl.CopyTable(transferParameters, sourceConnector.ConnectionString, targetConnector.ConnectionString);
                }
                else if (sourceConnector.SourceType == "File")
                {
                    if (sourceConnector.DataType == "Logs")
                    {
                        LASLoader ls = new LASLoader(fileStorageService);
                        await ls.LoadLASFile(sourceConnector, targetConnector, transferParameters.Table, referenceJson);
                    }
                    else
                    {
                        CSVLoader cl = new CSVLoader(fileStorageService);
                        await cl.LoadCSVFile(sourceConnector, targetConnector, transferParameters.Table);
                    }
                }
                else
                {
                    return BadRequest("Not valid source type");
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.ToString());
            }

            string message = $"{transferParameters.Table} has been copied";
            return Ok(message);
        }

        [HttpPost("remote")]
        public async Task<ActionResult<string>> CopyRemote(TransferParameters transferParameters)
        {
            try
            {
                string tmpConnString = Request.Headers["AzureStorageConnection"];
                queueService.SetConnectionString(tmpConnString);
                string message = JsonConvert.SerializeObject(transferParameters);
                queueService.InsertMessage(queueName, message);
            }
            catch (Exception)
            {
                return BadRequest("Problems with data transfer queue");
            }
            
            string response= $"{transferParameters.Table} has started on remote computer";
            return Ok(response);
        }

        [HttpDelete("{target}/{table}")]
        public async Task<ActionResult> DeleteTable(string target, string table)
        {
            string message = "";
            try
            {
                string tmpConnString = Request.Headers["AzureStorageConnection"];
                tableStorageService.SetConnectionString(tmpConnString);
                SourceEntity entity = await tableStorageService.GetTableRecord<SourceEntity>(container, target);
                ConnectParameters connector = mapper.Map<ConnectParameters>(entity);
                DbUtilities dbConn = new DbUtilities();
                dbConn.OpenConnection(connector);
                dbConn.DBDelete(table);
                message = $"{table} has been cleared";
                dbConn.CloseConnection();
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return BadRequest(message);
            }

            return Ok(message);
            //return NoContent();
        }
    }
}
