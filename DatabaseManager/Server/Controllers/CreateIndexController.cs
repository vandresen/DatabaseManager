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

namespace DatabaseManager.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CreateIndexController : ControllerBase
    {
        private readonly string connectionString;
        private readonly string container = "sources";
        private readonly IWebHostEnvironment _env;

        public CreateIndexController(IConfiguration configuration, IWebHostEnvironment env)
        {
            connectionString = configuration.GetConnectionString("AzureStorageConnection");
            _env = env;
        }

        [HttpPost]
        public async Task<ActionResult<List<ParentIndexNodes>>> CreateParentNodes(CreateIndexParameters iParameters)
        {
            if (iParameters == null) return BadRequest();
            List<ParentIndexNodes> nodes = new List<ParentIndexNodes>();
            try
            {
                IndexBuilder iBuilder = new IndexBuilder();
                string json = GetJsonIndexFile();
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

            try
            {
                IndexBuilder iBuilder = new IndexBuilder();
                string json = GetJsonIndexFile();
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
        
        private string GetJsonIndexFile()
        {
            string json = "";
            try
            {
                string contentRootPath = _env.ContentRootPath;
                string jsonFile = contentRootPath + @"\DataBase\WellBore.json";
                json = System.IO.File.ReadAllText(jsonFile);
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