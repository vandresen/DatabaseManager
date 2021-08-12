using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using DatabaseManager.Shared;
using DatabaseManager.Server.Helpers;
using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json.Linq;
using DatabaseManager.Server.Services;
using DatabaseManager.Server.Entities;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Cosmos.Table;
using DatabaseManager.Common.Helpers;

namespace DatabaseManager.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CreateIndexController : ControllerBase
    {
        private string connectionString;
        private readonly string container = "sources";
        private readonly string taxonomyShare = "taxonomy";
        private readonly IFileStorageService fileStorageService;
        private readonly ITableStorageService tableStorageService;
        private readonly ILogger<CreateIndexController> logger;
        private readonly IMapper mapper;
        private readonly IWebHostEnvironment _env;

        public CreateIndexController(IConfiguration configuration,
            IFileStorageService fileStorageService,
            ITableStorageService tableStorageService,
            ILogger<CreateIndexController> logger,
            IMapper mapper,
            IWebHostEnvironment env)
        {
            connectionString = configuration.GetConnectionString("AzureStorageConnection");
            this.fileStorageService = fileStorageService;
            this.tableStorageService = tableStorageService;
            this.logger = logger;
            this.mapper = mapper;
            _env = env;
        }

        [HttpGet]
        public async Task<ActionResult<List<string>>> GetTaxonomies()
        {
            try
            {
                string tmpConnString = Request.Headers["AzureStorageConnection"];
                fileStorageService.SetConnectionString(tmpConnString);
                List<string>  files = await fileStorageService.ListFiles(taxonomyShare);
                return files;
            }
            catch (Exception)
            {
                return BadRequest();
            }
        }

        [HttpGet("{name}")]
        public async Task<ActionResult<CreateIndexParameters>> Get(string name)
        {
            string tmpConnString = Request.Headers["AzureStorageConnection"];
            fileStorageService.SetConnectionString(tmpConnString);
            CreateIndexParameters parms = new CreateIndexParameters();
            try
            {
                parms.Taxonomy = await fileStorageService.ReadFile("taxonomy", name);
                //parms.ConnectDefinition = await fileStorageService.ReadFile("connectdefinition", "PPDMDataAccess.json");
            }
            catch (Exception ex)
            {
                string errorMessage = ex.ToString();
                return NotFound(errorMessage);
            }

            return parms;
        }

        [HttpPost]
        public async Task<ActionResult> Create(CreateIndexParameters iParameters)
        {
            logger.LogInformation("CreateIndexController: Starting index create");
            string tmpConnString = Request.Headers["AzureStorageConnection"];
            if (iParameters == null) return BadRequest();
            if (string.IsNullOrEmpty(iParameters.Taxonomy)) return BadRequest("Taxonomy not selected");
            
            try
            {
                Sources sr = new Sources(tmpConnString);
                ConnectParameters target = await sr.GetSourceParameters(iParameters.TargetName);
                ConnectParameters source = await sr.GetSourceParameters(iParameters.SourceName);

                Indexer index = new Indexer(tmpConnString);
                int parentNodes = await index.Initialize(target, source, iParameters.Taxonomy);

                List<ParentIndexNodes> nodes = await index.IndexParent(parentNodes);

                for (int j = 0; j < nodes.Count; j++)
                {
                    ParentIndexNodes node = nodes[j];
                    for (int i = 0; i < node.NodeCount; i++)
                    {
                        await index.IndexChildren(j, i, node.ParentNodeId);
                    }
                }

                index.CloseIndex();
            }
            catch (Exception ex)
            {
                logger.LogInformation($"CreateIndexController: Error message = {ex}");
                return BadRequest(ex.ToString());
            }

            return NoContent();
        }
        
    }
}