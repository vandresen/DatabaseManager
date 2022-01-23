using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DatabaseManager.Common.Helpers;
using DatabaseManager.Server.Services;
using DatabaseManager.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace DatabaseManager.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IndexController : ControllerBase
    {
        private string connectionString;

        public IndexController(IConfiguration configuration)
        {
            connectionString = configuration.GetConnectionString("AzureStorageConnection");
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
    }
}