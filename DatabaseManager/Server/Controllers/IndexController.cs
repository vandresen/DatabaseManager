using DatabaseManager.Common.Data;
using DatabaseManager.Common.DBAccess;
using DatabaseManager.Common.Helpers;
using DatabaseManager.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DatabaseManager.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IndexController : ControllerBase
    {
        private string connectionString;
        private readonly DapperDataAccess _dp;
        private readonly IIndexDBAccess _indexData;

        public IndexController(IConfiguration configuration)
        {
            connectionString = configuration.GetConnectionString("AzureStorageConnection");
            _dp = new DapperDataAccess();
            _indexData = new IndexDBAccess(_dp);
        }

        [HttpGet("{source}")]
        public async Task<ActionResult<List<DmsIndex>>> Get(string source)
        {
            
            try
            {
                string storageAccount = Common.Helpers.Common.GetStorageKey(Request);
                IndexManagement im = new IndexManagement(storageAccount);
                string responseMessage = await im.GetIndexData(source);
                List<DmsIndex> index = JsonConvert.DeserializeObject<List<DmsIndex>>(responseMessage);
                return index;
            }
            catch (Exception ex)
            {
                return BadRequest(ex.ToString());
            }
        }

        [HttpGet("{source}/{id}")]
        public async Task<ActionResult<string>> GetChildren(string source, int id)
        {
            string storageAccount = Common.Helpers.Common.GetStorageKey(Request);
            IndexManagement im = new IndexManagement(storageAccount);
            string result = await im.GetIndexItem(source, id);
            return result;
        }

        [HttpGet("GetIndexRoot/{source}")]
        public async Task<ActionResult<string>> GetIndexRoot(string source)
        {
            string result = "";
            string storageAccount = Request.Headers["AzureStorageConnection"];
            ConnectParameters connector = await DatabaseManager.Common.Helpers.Common.GetConnectParameters(storageAccount, source);
            IndexModel root = await _indexData.GetIndexRoot(connector.ConnectionString);
            result = JsonConvert.SerializeObject(root);
            return result;
        }

        [HttpGet("GetIndexTaxonomy/{source}")]
        public async Task<ActionResult<string>> GetIndexTaxonomy(string source)
        {
            string storageAccount = Request.Headers["AzureStorageConnection"];
            //string storageAccount = Common.Helpers.Common.GetStorageKey(Request);
            IndexManagement im = new IndexManagement(storageAccount);
            string result = await im.GetIndexTaxonomy(source);
            return result;
        }

        [HttpGet("GetTaxonomyFile/{name}")]
        public async Task<ActionResult<string>> GetTaxonomyFile(string name)
        {
            try
            {
                string storageAccount = Common.Helpers.Common.GetStorageKey(Request);
                IndexManagement im = new IndexManagement(storageAccount);
                string result = await im.GetTaxonomyFile(name);
                return result;
            }
            catch (Exception ex)
            {
                return BadRequest(ex.ToString());
            }
            
        }

        [HttpGet("GetSingleIndexItem/{source}/{id}")]
        public async Task<ActionResult<string>> GetSingleIndexItem(string source, int id)
        {
            string storageAccount = Request.Headers["AzureStorageConnection"];
            //string storageAccount = Common.Helpers.Common.GetStorageKey(Request);
            IndexManagement im = new IndexManagement(storageAccount);
            string result = await im.GetSingleIndexItem(source, id);
            return result;
        }

        [HttpPost("{name}")]
        public async Task<ActionResult<List<DmsIndex>>> Save(string name, List<IndexFileDefinition> ifd)
        {
            try
            {
                string storageAccount = Common.Helpers.Common.GetStorageKey(Request);
                IndexManagement im = new IndexManagement(storageAccount);
                string json = JsonConvert.SerializeObject(ifd, Formatting.Indented);
                await im.SaveTaxonomyFile(name, json);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.ToString());
            }
        }
    }
}