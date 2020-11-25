using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DatabaseManager.Server.Entities;
using DatabaseManager.Server.Helpers;
using DatabaseManager.Server.Services;
using DatabaseManager.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DatabaseManager.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DataTransferController : ControllerBase
    {
        private readonly ITableStorageService tableStorageService;
        private readonly IFileStorageService fileStorageService;
        private readonly IMapper mapper;
        private readonly string container = "sources";

        public DataTransferController(ITableStorageService tableStorageService,
            IFileStorageService fileStorageService,
            IMapper mapper)
        {
            this.tableStorageService = tableStorageService;
            this.fileStorageService = fileStorageService;
            this.mapper = mapper;
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
                SourceEntity entity = await tableStorageService.GetTableRecord<SourceEntity>(container, transferParameters.SourceName);
                ConnectParameters sourceConnector = mapper.Map<ConnectParameters>(entity);
                entity = await tableStorageService.GetTableRecord<SourceEntity>(container, transferParameters.TargetName);
                ConnectParameters targetConnector = mapper.Map<ConnectParameters>(entity);

                if (sourceConnector.SourceType == "DataBase")
                {
                    DatabaseLoader dl = new DatabaseLoader();
                    dl.CopyTable(transferParameters, sourceConnector.ConnectionString, targetConnector.ConnectionString);
                }
                else if (sourceConnector.SourceType == "File")
                {
                    if (sourceConnector.DataType == "Logs")
                    {
                        LASLoader ls = new LASLoader(fileStorageService, tableStorageService);
                        await ls.LoadLASFile(sourceConnector, targetConnector, transferParameters.Table);
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

        [HttpDelete("{source}/{table}")]
        public async Task<ActionResult> DeleteTable(string source, string table)
        {
            string message = "";
            try
            {
                string tmpConnString = Request.Headers["AzureStorageConnection"];
                tableStorageService.SetConnectionString(tmpConnString);
                SourceEntity entity = await tableStorageService.GetTableRecord<SourceEntity>(container, source);
                ConnectParameters connector = mapper.Map<ConnectParameters>(entity);
                DbUtilities dbConn = new DbUtilities();
                dbConn.OpenConnection(connector);
                dbConn.DBDelete(table);
                message = $"{table} has been cleared";
                dbConn.CloseConnection();
            }
            catch (Exception ex)
            {
                return BadRequest();
            }

            return Ok(message);
            //return NoContent();
        }
    }
}
