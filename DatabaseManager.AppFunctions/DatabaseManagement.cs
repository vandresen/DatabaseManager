using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using DatabaseManager.Shared;
using DatabaseManager.Common.Helpers;

namespace DatabaseManager.AppFunctions
{
    public static class DatabaseManagement
    {
        [FunctionName("DataModelCreate")]
        public static async Task<IActionResult> ModelCreate(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("DataModelCreate: Starting");
            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                DataModelParameters dmParameters = JsonConvert.DeserializeObject<DataModelParameters>(requestBody);
                if (dmParameters == null)
                {
                    log.LogError("DataModelCreate: error missing data model parameters");
                    return new BadRequestObjectResult("Error missing data model parameters");
                }
                string storageAccount = Common.Helpers.Common.GetStorageKey(req);
                DataModelManagement dm = new DataModelManagement(storageAccount, null);
                await dm.DataModelCreate(dmParameters);
            }
            catch (Exception ex)
            {
                log.LogError($"DataModelCreate: {ex}");
                return new BadRequestObjectResult($"Error creating data model: {ex}");
            }

            log.LogInformation("DataModelCreate: Complete");
            return new OkObjectResult("OK");
        }
    }
}
