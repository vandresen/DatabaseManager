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
using DatabaseManager.Shared;
using System.Collections.Generic;

namespace DatabaseManager.AppFunctions
{
    public static class DataQcOperations
    {
        [FunctionName("GetDataQcResults")]
        public static async Task<IActionResult> GetResults(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("GetDataQcResults: Starting.");
            string jsonResult = "OK";

            try
            {
                string storageAccount = Common.Helpers.Common.GetStorageKey(req);
                string source = Common.Helpers.Common.GetQueryString(req, "name");
                DataQC qc = new DataQC(storageAccount);
                DataQCParameters qcParms = new DataQCParameters();
                qcParms.DataConnector = source;
                List<QcResult> qcResults = new List<QcResult>();
                qcResults = await qc.GetQCRules(qcParms);
                jsonResult = JsonConvert.SerializeObject(qcResults, Formatting.Indented);
            }
            catch (Exception ex)
            {
                log.LogError($"GetDataQcResults: Error getting QCResults: {ex}");
                return new BadRequestObjectResult($"Error getting QCResults: {ex}");
            }

            

            log.LogInformation("GetDataQcResults: Complete.");
            return new OkObjectResult(jsonResult);
        }

        [FunctionName("GetDataQcResult")]
        public static async Task<IActionResult> GetResult(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("GetDataQcResults: Starting.");
            string jsonResult = "OK";

            try
            {
                string storageAccount = Common.Helpers.Common.GetStorageKey(req);
                string source = Common.Helpers.Common.GetQueryString(req, "name");
                int id = Common.Helpers.Common.GetIntFromWebQuery(req, "id");
                DataQC qc = new DataQC(storageAccount);
                DataQCParameters qcParms = new DataQCParameters();
                qcParms.DataConnector = source;
                List<QcResult> qcResults = new List<QcResult>();
                jsonResult = await qc.GetQCFailures(source, id);
            }
            catch (Exception ex)
            {
                log.LogError($"GetDataQcResults: Error getting QCResults: {ex}");
                return new BadRequestObjectResult($"Error getting QCResults: {ex}");
            }



            log.LogInformation("GetDataQcResults: Complete.");
            return new OkObjectResult(jsonResult);
        }
    }
}
