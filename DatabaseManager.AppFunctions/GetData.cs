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
        [FunctionName("GetDataOpsData")]
        public static async Task<IActionResult> DataOpsData(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ExecutionContext context,
            ILogger log)
        {
            log.LogInformation("GetDataOpsData: Starting.");
            string jsonResult = "OK";

            var headers = req.Headers;
            string storageAccount = headers.FirstOrDefault(x => x.Key == "AzureStorageConnection").Value;
            DataOps dops = new DataOps(storageAccount);
            List<DataOpsPipes> pipes = await dops.GetDataOpsPipes();
            jsonResult = JsonConvert.SerializeObject(pipes, Formatting.Indented);

            //ConfigurationInfo configuration = Utilities.GetConfigurations(context);

            //AzureStorageService azureConn = new AzureStorageService();
            //azureConn.OpenConnection(configuration.AzureStorageConnection);
            //List<SP500Stocks> stocks = azureConn.GetStocks();
            //string jsonResult = JsonConvert.SerializeObject(stocks, Formatting.Indented);

            log.LogInformation("GetDataOpsData: Completed.");
            return new OkObjectResult(jsonResult);
        }
    }
}
