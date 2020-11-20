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
using Microsoft.Extensions.Configuration;

namespace DatabaseManager.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DeleteController : ControllerBase
    {
        private string connectionString;
        private readonly string container = "sources";
        private readonly IFileStorageService fileStorageService;
        private readonly ITableStorageService tableStorageService;
        private readonly IMapper mapper;


        public DeleteController(IConfiguration configuration,
            IFileStorageService fileStorageService,
            ITableStorageService tableStorageService,
            IMapper mapper)
        {
            connectionString = configuration.GetConnectionString("AzureStorageConnection");
            this.fileStorageService = fileStorageService;
            this.tableStorageService = tableStorageService;
            this.mapper = mapper;
        }

        [HttpPost]
        public async Task<ActionResult> Delete(TransferParameters transferParameters)
        {
            DbUtilities dbConn = new DbUtilities();
            string table = transferParameters.Table;
            try
            {
                string tmpConnString = Request.Headers["AzureStorageConnection"];
                fileStorageService.SetConnectionString(tmpConnString);
                tableStorageService.SetConnectionString(tmpConnString);
                SourceEntity entity = await tableStorageService.GetTableRecord<SourceEntity>(container, transferParameters.TargetName);
                ConnectParameters connector = mapper.Map<ConnectParameters>(entity);
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