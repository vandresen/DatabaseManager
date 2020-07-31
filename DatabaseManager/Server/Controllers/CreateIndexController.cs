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

namespace DatabaseManager.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CreateIndexController : ControllerBase
    {
        private readonly string connectionString;
        private readonly string container = "sources";
        private readonly string taxonomyShare = "taxonomy";
        private readonly IWebHostEnvironment _env;
        private CloudFileShare share;

        public CreateIndexController(IConfiguration configuration, IWebHostEnvironment env)
        {
            connectionString = configuration.GetConnectionString("AzureStorageConnection");
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
                //CloudStorageAccount account = CloudStorageAccount.Parse(connectionString);
                //CloudFileClient fileClient = account.CreateCloudFileClient();
                //CloudFileShare share = account.CreateCloudFileClient().GetShareReference(taxonomyShare);
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

        [HttpPost]
        public async Task<ActionResult<List<ParentIndexNodes>>> CreateParentNodes(CreateIndexParameters iParameters)
        {
            if (iParameters == null) return BadRequest();
            if (string.IsNullOrEmpty(iParameters.TaxonomyFile)) return BadRequest();
            List<ParentIndexNodes> nodes = new List<ParentIndexNodes>();
            try
            {
                IndexBuilder iBuilder = new IndexBuilder();
                string json = GetJsonIndexFile(iParameters.TaxonomyFile);
                ConnectParameters connector = Common.GetConnectParameters(connectionString, container, 
                    iParameters.DataConnector);
                iBuilder.InitializeIndex(connector, json);
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
            if (string.IsNullOrEmpty(iParams.TaxonomyFile)) return BadRequest();
            try
            {
                IndexBuilder iBuilder = new IndexBuilder();
                string json = GetJsonIndexFile(iParams.TaxonomyFile);
                ConnectParameters connector = Common.GetConnectParameters(connectionString, container,
                    iParams.DataConnector);
                iBuilder.InitializeIndex(connector, json);
                iBuilder.PopulateIndex(iParams.ParentNodeNumber, iParams.ParentNumber, iParams.ParentNodeId);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.ToString());
            }

            return NoContent();
        }
        
        private string GetJsonIndexFile(string jsonFile)
        {
            string json = "";
            try
            {
                if (share.Exists())
                {
                    CloudFileDirectory rootDir = share.GetRootDirectoryReference();
                    CloudFile file = rootDir.GetFileReference(jsonFile);
                    if (file.Exists())
                    {
                        json = file.DownloadTextAsync().Result;
                    }
                    else
                    {
                        Exception error = new Exception("File does not exist ");
                        throw error;
                    }
                }
                else
                {
                    Exception error = new Exception("Share does not exist ");
                    throw error;
                }
                    string contentRootPath = _env.ContentRootPath;
            }
            catch (Exception ex)
            {
                Exception error = new Exception("Read index file error: ", ex);
                throw error;
            }
            return json;
        }
    }
}