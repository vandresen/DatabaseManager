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
    public static class ReportOperations
    {
        [FunctionName("GetResults")]
        public static async Task<IActionResult> Results(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("GetResults: Starting.");
            string jsonResult = "OK";

            try
            {
                string storageAccount = Common.Helpers.Common.GetStorageKey(req);
                string source = Common.Helpers.Common.GetQueryString(req, "name");
                DataQC qc = new DataQC(storageAccount);
                List<QcResult> results = await qc.GetResults(source);
                jsonResult = JsonConvert.SerializeObject(results, Formatting.Indented);
            }
            catch (Exception ex)
            {
                log.LogError($"GetResults: Error getting QCResults: {ex}");
                return new BadRequestObjectResult($"Error getting QCResults: {ex}");
            }



            log.LogInformation("GetResults: Complete.");
            return new OkObjectResult(jsonResult);
        }

        [FunctionName("GetResult")]
        public static async Task<IActionResult> Result(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("GetResult: Starting.");
            string jsonResult = "OK";

            try
            {
                string storageAccount = Common.Helpers.Common.GetStorageKey(req);
                string source = Common.Helpers.Common.GetQueryString(req, "name");
                int id = Common.Helpers.Common.GetIntFromWebQuery(req, "id");
                DataQC qc = new DataQC(storageAccount);
                List<DmsIndex> results = await qc.GetResult(source, id);
                jsonResult = JsonConvert.SerializeObject(results, Formatting.Indented);
            }
            catch (Exception ex)
            {
                log.LogError($"GetResult: Error getting QCResults: {ex}");
                return new BadRequestObjectResult($"Error getting QCResults: {ex}");
            }



            log.LogInformation("GetResult: Complete.");
            return new OkObjectResult(jsonResult);
        }

        [FunctionName("GetDataQcResults")]
        public static async Task<IActionResult> DataQcResults(
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
        public static async Task<IActionResult> DataQcResult(
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