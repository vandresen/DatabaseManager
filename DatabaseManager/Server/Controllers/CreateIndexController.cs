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
        private readonly string connectionString;
        private readonly string container = "sources";
        private readonly string taxonomyShare = "taxonomy";
        private readonly IFileStorageService fileStorageService;
        private readonly IWebHostEnvironment _env;
        private CloudFileShare share;

        public CreateIndexController(IConfiguration configuration,
            IFileStorageService fileStorageService,
            IWebHostEnvironment env)
        {
            connectionString = configuration.GetConnectionString("AzureStorageConnection");
            this.fileStorageService = fileStorageService;
            _env = env;
            CloudStorageAccount account = CloudStorageAccount.Parse(connectionString);
            CloudFileClient fileClient = account.CreateCloudFileClient();
            share = account.CreateCloudFileClient().GetShareReference(taxonomyShare);
        }

        [HttpGet]
        public async Task<ActionResult<List<string>>> GetTaxonomies()
        {
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
            CreateIndexParameters parms = new CreateIndexParameters();
            try
            {
                parms.Taxonomy = await fileStorageService.ReadFile("taxonomy", name);
                parms.ConnectDefinition = await fileStorageService.ReadFile("connectdefinition", "PPDMDataAccess.json");
            }
            catch (Exception)
            {
                return NotFound();
            }

            return parms;
        }

        [HttpPost]
        public async Task<ActionResult<List<ParentIndexNodes>>> CreateParentNodes(CreateIndexParameters iParameters)
        {
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
                for (int k = 0; k < parentNodes; k++)
                {
                    JToken token = iBuilder.JsonIndexArray[k];
                    int parentCount = iBuilder.GetObjectCount(token, k);
                    if (parentCount > 0)
                    {
                        int parentNodeId = iBuilder.CreateParentNodeIndex();
                        nodes.Add(new ParentIndexNodes()
                        {
                            NodeCount = parentCount,
                            ParentNodeId = parentNodeId,
                            Name = (string)token["DataName"]
                        });
                    }
                }
                iBuilder.CloseIndex();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.ToString());
            }
            return nodes;
        }

        [HttpPost("children")]
        public async Task<ActionResult> CreateChildren(CreateIndexParameters iParams)
        {
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
                iBuilder.PopulateIndex(iParams.ParentNodeNumber, iParams.ParentNumber, iParams.ParentNodeId);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.ToString());
            }

            return NoContent();
        }
        
        //private string GetJsonIndexFile(string jsonFile)
        //{
        //    string json = "";
        //    try
        //    {
        //        if (share.Exists())
        //        {
        //            CloudFileDirectory rootDir = share.GetRootDirectoryReference();
        //            CloudFile file = rootDir.GetFileReference(jsonFile);
        //            if (file.Exists())
        //            {
        //                json = file.DownloadTextAsync().Result;
        //            }
        //            else
        //            {
        //                Exception error = new Exception("File does not exist ");
        //                throw error;
        //            }
        //        }
        //        else
        //        {
        //            Exception error = new Exception("Share does not exist ");
        //            throw error;
        //        }
        //            string contentRootPath = _env.ContentRootPath;
        //    }
        //    catch (Exception ex)
        //    {
        //        Exception error = new Exception("Read index file error: ", ex);
        //        throw error;
        //    }
        //    return json;
        //}
    }
}