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
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.File;
using DatabaseManager.Server.Services;

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
        private readonly IWebHostEnvironment _env;
        //private CloudFileShare share;

        public CreateIndexController(IConfiguration configuration,
            IFileStorageService fileStorageService,
            IWebHostEnvironment env)
        {
            connectionString = configuration.GetConnectionString("AzureStorageConnection");
            this.fileStorageService = fileStorageService;
            _env = env;
        }

        [HttpGet]
        public async Task<ActionResult<List<string>>> GetTaxonomies()
        {
            string tmpConnString = Request.Headers["AzureStorageConnection"];
            if (!string.IsNullOrEmpty(tmpConnString)) connectionString = tmpConnString;
            if (string.IsNullOrEmpty(connectionString)) return NotFound("Connection string is not set");
            CloudFileShare share = GetAzureStorageShare();
            List<string> files = new List<string>();
            try
            {
                IEnumerable<IListFileItem> fileList = share.GetRootDirectoryReference().ListFilesAndDirectories();
                foreach (IListFileItem listItem in fileList)
                {
                    if (listItem.GetType() == typeof(CloudFile))
                    {
                        files.Add(listItem.Uri.Segments.Last());
                    }
                }
            }
            catch (Exception)
            {
                return BadRequest();
            }
            return files;
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
                parms.ConnectDefinition = await fileStorageService.ReadFile("connectdefinition", "PPDMDataAccess.json");
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
            string tmpConnString = Request.Headers["AzureStorageConnection"];
            if (!string.IsNullOrEmpty(tmpConnString)) connectionString = tmpConnString;
            if (string.IsNullOrEmpty(connectionString)) return NotFound("Connection string is not set");
            //CloudFileShare share = GetAzureStorageShare();
            if (iParameters == null) return BadRequest();
            if (string.IsNullOrEmpty(iParameters.Taxonomy)) return BadRequest();
            if (string.IsNullOrEmpty(iParameters.ConnectDefinition)) return BadRequest();
            List<ParentIndexNodes> nodes = new List<ParentIndexNodes>();
            try
            {
                IndexBuilder iBuilder = new IndexBuilder();
                string jsonTaxonomy = iParameters.Taxonomy;
                string jsonConnectDef = iParameters.ConnectDefinition;
                ConnectParameters connector = Common.GetConnectParameters(connectionString, container, 
                    iParameters.DataConnector);
                iBuilder.InitializeIndex(connector, jsonTaxonomy, jsonConnectDef);
                iBuilder.CreateRoot();
                int parentNodes = iBuilder.JsonIndexArray.Count;
                int nodeId = 0;
                for (int k = 0; k < parentNodes; k++)
                {
                    JToken token = iBuilder.JsonIndexArray[k];
                    int parentCount = iBuilder.GetObjectCount(token, k);
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
                        iBuilder.PopulateIndex(j, i, node.ParentNodeId);
                    }
                }

                iBuilder.CloseIndex();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.ToString());
            }

            return NoContent();
        }

        [HttpPost("children")]
        public async Task<ActionResult> CreateChildren(CreateIndexParameters iParams)
        {
            string tmpConnString = Request.Headers["AzureStorageConnection"];
            if (!string.IsNullOrEmpty(tmpConnString)) connectionString = tmpConnString;
            if (string.IsNullOrEmpty(connectionString)) return NotFound("Connection string is not set");
            CloudFileShare share = GetAzureStorageShare();
            if (iParams == null) return BadRequest();
            if (string.IsNullOrEmpty(iParams.Taxonomy)) return BadRequest();
            if (string.IsNullOrEmpty(iParams.ConnectDefinition)) return BadRequest();
            try
            {
                IndexBuilder iBuilder = new IndexBuilder();
                string jsonTaxonomy = iParams.Taxonomy;
                string jsonConnectDef = iParams.ConnectDefinition;
                ConnectParameters connector = Common.GetConnectParameters(connectionString, container,
                    iParams.DataConnector);
                iBuilder.InitializeIndex(connector, jsonTaxonomy, jsonConnectDef);
                //iBuilder.PopulateIndex(iParams.ParentNodeNumber, iParams.ParentNumber, iParams.ParentNodeId);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.ToString());
            }

            return NoContent();
        }
        
        private CloudFileShare GetAzureStorageShare()
        {
            CloudStorageAccount account = CloudStorageAccount.Parse(connectionString);
            CloudFileClient fileClient = account.CreateCloudFileClient();
            CloudFileShare share = account.CreateCloudFileClient().GetShareReference(taxonomyShare);
            return share;
        }
    }
}