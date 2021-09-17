using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using DatabaseManager.Common.Helpers;

namespace DatabaseManager.AppFunctions
{
    public static class Index
    {
        [FunctionName("GetTaxonomy")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("GetTaxonomy: Starting");
            string responseMessage = "";
            try
            {
                string storageAccount = Common.Helpers.Common.GetStorageKey(req, log);
                IndexManagement im = new IndexManagement(storageAccount);
            }
            catch (Exception)
            {

                throw;
            }

            log.LogInformation("GetTaxonomy: Complete");
            return new OkObjectResult(responseMessage);
        }
    }
}
