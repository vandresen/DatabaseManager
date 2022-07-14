using DatabaseManager.Common.Helpers;
using DatabaseManager.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DatabaseManager.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CreateIndexController : ControllerBase
    {
        private string connectionString;
        private readonly ILogger<CreateIndexController> logger;

        public CreateIndexController(IConfiguration configuration,
            ILogger<CreateIndexController> logger)
        {
            connectionString = configuration.GetConnectionString("AzureStorageConnection");
            this.logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<List<IndexFileList>>> GetTaxonomies()
        {
            try
            {
                string storageAccount = Common.Helpers.Common.GetStorageKey(Request);
                IndexManagement im = new IndexManagement(storageAccount);
                string responseMessage = await im.GetTaxonomies();
                List<IndexFileList> indexParms = JsonConvert.DeserializeObject<List<IndexFileList>>(responseMessage);
                //List<string> files = indexParms.Select(item => item.Name).ToList();
                return indexParms;
            }
            catch (Exception)
            {
                return BadRequest();
            }
        }

        [HttpPost]
        public async Task<ActionResult> Create(CreateIndexParameters iParameters)
        {
            logger.LogInformation("CreateIndexController: Starting index create");
            try
            {
                string storageAccount = Common.Helpers.Common.GetStorageKey(Request);
                IndexManagement im = new IndexManagement(storageAccount);
                await im.CreateIndex(iParameters);
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