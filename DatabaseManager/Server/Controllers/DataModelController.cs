using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using DatabaseManager.Shared;
using Microsoft.AspNetCore.Hosting;
using DatabaseManager.Server.Entities;
using System.Data;
using Newtonsoft.Json;
using DatabaseManager.Server.Services;
using Microsoft.Extensions.Logging;
using AutoMapper;
using DatabaseManager.Common.Helpers;

namespace DatabaseManager.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DataModelController : ControllerBase
    {
        private readonly IFileStorageService fileStorageService;
        private readonly ITableStorageService tableStorageService;
        private readonly IMapper mapper;
        private readonly ILogger<DataModelController> logger;
        private readonly IWebHostEnvironment _env;
        private string connectionString;
        private string _credentials;
        private string _blobStorage;
        private string _secret;
        private readonly string _contentRootPath;
        private readonly string container = "sources";

        public DataModelController(IConfiguration configuration,
            IFileStorageService fileStorageService,
            ITableStorageService tableStorageService,
            IMapper mapper,
            ILogger<DataModelController> logger,
            IWebHostEnvironment env)
        {
            connectionString = configuration.GetConnectionString("AzureStorageConnection");
            _credentials = configuration["BlobCredential"];
            _secret = configuration["BlobSecret"];
            _blobStorage = configuration["BlobStorage"];
            this.fileStorageService = fileStorageService;
            this.tableStorageService = tableStorageService;
            this.mapper = mapper;
            this.logger = logger;
            _env = env;
            _contentRootPath = _env.ContentRootPath;
        }

        [HttpPost]
        public async Task<ActionResult<string>> DataModelCreate(DataModelParameters dmParameters)
        {
            logger.LogInformation("Starting data model create");
            if (dmParameters == null) return BadRequest();
            try
            {
                string storageAccount = Common.Helpers.Common.GetStorageKey(Request);
                DataModelManagement dmm = new DataModelManagement(storageAccount, _contentRootPath);
                await dmm.DataModelCreate(dmParameters);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.ToString());
            }
            logger.LogInformation("Data model create Complete");
            return Ok($"OK");
        }
    }
}