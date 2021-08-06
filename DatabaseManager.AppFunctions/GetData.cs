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
using System.Collections.Generic;
using DatabaseManager.Shared;

namespace DatabaseManager.AppFunctions
{
    public static class GetData
    {
        [FunctionName("GetDataOpsList")]
        public static async Task<IActionResult> DataOpsList(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ExecutionContext context,
            ILogger log)
        {
            log.LogInformation("GetDataOpsList: Starting.");
            string jsonResult = "OK";

            var headers = req.Headers;
            string storageAccount = headers.FirstOrDefault(x => x.Key == "azurestorageconnection").Value;
            if (string.IsNullOrEmpty(storageAccount))
            {
                log.LogError("GetDataOpsList: error, missing azure storage account");
                return new BadRequestObjectResult("Missing azure storage account in http header");
            }
            DataOps dops = new DataOps(storageAccount);
            List<DataOpsPipes> pipes = await dops.GetDataOpsPipes();
            jsonResult = JsonConvert.SerializeObject(pipes, Formatting.Indented);

            log.LogInformation("GetDataOpsList: Completed.");
            return new OkObjectResult(jsonResult);
        }

        [FunctionName("GetDataOpsPipe")]
        public static async Task<IActionResult> DataOpsPipe(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ExecutionContext context,
            ILogger log)
        {
            log.LogInformation("GetDataOpsPipe: Starting.");
            string jsonResult = "OK";

            string pipeName = req.Query["name"];
            //string pipeName = await new StreamReader(req.Body).ReadToEndAsync();
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
                log.LogError("GetDataOpsPipe: error, missing azure storage account");
                return new BadRequestObjectResult("Missing azure storage account in http header");
            }
            DataOps dops = new DataOps(storageAccount);
            List<PipeLine> dataOps = await dops.GetDataOpsPipe(pipeName);
            jsonResult = JsonConvert.SerializeObject(dataOps, Formatting.Indented);

            log.LogInformation("GetDataOpsPipe: Completed.");
            return new OkObjectResult(jsonResult);
        }
    }
}
