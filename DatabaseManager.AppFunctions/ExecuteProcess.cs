using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Linq;
using DatabaseManager.Common.Helpers;

namespace DatabaseManager.AppFunctions
{
    public static class ExecuteProcess
    {
        [FunctionName("ExecuteDataOps")]
        public static async Task<IActionResult> DataOps(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("ExecuteDataOps: Starting.");
            string jsonResult = "OK";

            string pipeName = await new StreamReader(req.Body).ReadToEndAsync();
            log.LogInformation($"ExecuteDataOps: {pipeName}");
            if (string.IsNullOrEmpty(pipeName))
            {
                log.LogError("ExecuteDataOps: error, missing pipe name");
                return new BadRequestObjectResult("Missing pipe name in body");
            }

            var headers = req.Headers;
            string storageAccount = headers.FirstOrDefault(x => x.Key == "azurestorageconnection").Value;
            if (string.IsNullOrEmpty(storageAccount))
            {
                log.LogError("GetDataOpsData: error, missing azure storage account");
                return new BadRequestObjectResult("Missing azure storage account in http header");
            }

            DataOps dops = new DataOps(storageAccount);
            //List<DataOpsPipes> pipes = await dops.GetDataOpsPipes();
            //jsonResult = JsonConvert.SerializeObject(pipes, Formatting.Indented);

            log.LogInformation("ExecuteDataOps: Completed.");
            return new OkObjectResult(jsonResult);
        }
    }
}
