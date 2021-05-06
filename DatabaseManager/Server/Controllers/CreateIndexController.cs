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
            List<ParentIndexNodes> nodes = new List<ParentIndexNodes>();
            try
            {
                fileStorageService.SetConnectionString(tmpConnString);
                tableStorageService.SetConnectionString(tmpConnString);
                
                string jsonTaxonomy = await fileStorageService.ReadFile("taxonomy", iParameters.Taxonomy);
                string jsonConnectDef = await fileStorageService.ReadFile("connectdefinition", "PPDMDataAccess.json");

                SourceEntity entity = await tableStorageService.GetTableRecord<SourceEntity>(container, iParameters.TargetName);
                if (entity == null) return BadRequest($"No source with name {iParameters.TargetName}");
                ConnectParameters target = mapper.Map<ConnectParameters>(entity);
                ConnectParameters source = new ConnectParameters();
                if (iParameters.TargetName == iParameters.SourceName)
                {
                    source = target;
                }
                else
                {
                    entity = await tableStorageService.GetTableRecord<SourceEntity>(container, iParameters.SourceName);
                    source = mapper.Map<ConnectParameters>(entity);
                }

                IndexBuilder iBuilder = new IndexBuilder();
                
                if (source.SourceType == "DataBase")
                {
                    iBuilder = new IndexBuilder(new DBDataAccess());
                }
                else
                {
                    source.ConnectionString = tmpConnString;
                    if (source.DataType == "Logs")
                    {
                        iBuilder = new IndexBuilder(new LASDataAccess(fileStorageService));
                    }
                    else
                    {
                        iBuilder = new IndexBuilder(new CSVDataAccess(fileStorageService));
                    }
                    
                }

                logger.LogInformation("CreateIndexController: start indexing");
                target.DataAccessDefinition = jsonConnectDef;
                iBuilder.InitializeIndex(target, source, jsonTaxonomy);
                iBuilder.CreateRoot(source);
                int parentNodes = iBuilder.JsonIndexArray.Count;
                int nodeId = 0;
                for (int k = 0; k < parentNodes; k++)
                {
                    JToken token = iBuilder.JsonIndexArray[k];
                    int parentCount = await iBuilder.GetObjectCount(token, k);
                    if (parentCount > 0)
                    {
                        nodeId++;
                        string strNodeId = $"/{nodeId}/";
                        iBuilder.CreateParentNodeIndex(strNodeId);
                        nodes.Add(new ParentIndexNodes()
                        {
                            NodeCount = parentCount,
                            ParentNodeId = strNodeId,
                            Name = (string)token["DataName"]
                        });
                    }
                }

                for (int j = 0; j < nodes.Count; j++)
                {
                    ParentIndexNodes node = nodes[j];
                    for (int i = 0; i < node.NodeCount; i++)
                    {
                        await iBuilder.PopulateIndex(j, i, node.ParentNodeId);
                    }
                }

                iBuilder.CloseIndex();
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