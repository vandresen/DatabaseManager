using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using DatabaseManager.Shared;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using AutoMapper;
using DatabaseManager.Common.Helpers;
using DatabaseManager.Common.Services;

namespace DatabaseManager.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DataModelController : ControllerBase
    {
        private readonly IFileStorageServiceCommon fileStorageService;
        private readonly ILogger<DataModelController> logger;
        private readonly IWebHostEnvironment _env;
        private string connectionString;
        private readonly string _contentRootPath;
        public DataModelController(IConfiguration configuration,
            IFileStorageServiceCommon fileStorageService,
            ILogger<DataModelController> logger,
            IWebHostEnvironment env)
        {
            connectionString = configuration.GetConnectionString("AzureStorageConnection");
            this.fileStorageService = fileStorageService;
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
                string storageAccount = DatabaseManager.Common.Helpers.Common.GetStorageKey(Request);
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