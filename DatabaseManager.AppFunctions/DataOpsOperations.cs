using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using DatabaseManager.AppFunctions.Helpers;
using DatabaseManager.Shared;
using DatabaseManager.Common.Helpers;
using System.Collections.Generic;

namespace DatabaseManager.AppFunctions
{
    public static class DataOpsOperations
    {
        [FunctionName("SavePipeline")]
        public static async Task<IActionResult> Create(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("SavePipeline: Starting.");

            try
            {
                string storageAccount = Utilities.GetAzureStorageConnection(req.Headers, log);
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                DataOpsPipes pipeName = JsonConvert.DeserializeObject<DataOpsPipes>(requestBody);
                if (pipeName == null)
                {
                    log.LogError("SavePipeline: error missing pipe ops parameters");
                    return new BadRequestObjectResult("Error missing pipe parameters");
                }
                if (string.IsNullOrEmpty(pipeName.Name))
                {
                    log.LogError("SavePipeline: error missing pipeline name");
                    return new BadRequestObjectResult("Error missing pipeline name");
                }
                string name = pipeName.Name;
                if (!name.EndsWith(".txt")) name = name + ".txt";
                log.LogInformation($"CreatePipeline: Pipe name {name}");
                DataOpsRepository dops = new DataOpsRepository(storageAccount);
                await dops.SavePipeline(name, "");
            }
            catch (Exception ex)
            {
                log.LogError($"SavePipeline: Error creating pipeline: {ex}");
                return new BadRequestObjectResult($"Error creating pipeline: {ex}");
            }
            string responseMessage = "OK";
            log.LogInformation("SavePipeline: Completed.");
            return new OkObjectResult(responseMessage);
        }

        [FunctionName("SavePipelineData")]
        public static async Task<IActionResult> SavePipeData(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("SavePipelineData: Starting.");

            try
            {
                string storageAccount = Utilities.GetAzureStorageConnection(req.Headers, log);
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                string name = req.Query["name"];
                if (string.IsNullOrEmpty(name))
                {
                    log.LogError("SavePipelineData: error, pipeline name is missing");
                    return new BadRequestObjectResult("Error missing pipeline name");
                }
                List<PipeLine> tubes = JsonConvert.DeserializeObject<List<PipeLine>>(requestBody);
                if (tubes == null)
                {
                    log.LogError("SavePipeline: error missing pipeline parameters");
                    return new BadRequestObjectResult("Error missing pipeline parameters");
                }
                if (!name.EndsWith(".txt")) name = name + ".txt";
                DataOpsRepository dops = new DataOpsRepository(storageAccount);
                await dops.SavePipeline(name, requestBody);
            }
            catch (Exception ex)
            {
                log.LogError($"SavePipelineData: Error creating pipeline: {ex}");
                return new BadRequestObjectResult($"Error creating pipeline: {ex}");
            }
            string responseMessage = "OK";
            log.LogInformation("SavePipelineData: Completed.");
            return new OkObjectResult(responseMessage);
        }

        [FunctionName("DeletePipeline")]
        public static async Task<IActionResult> Delete(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("DeletePipeline: Starting.");

            try
            {
                string storageAccount = Utilities.GetAzureStorageConnection(req.Headers, log);
                string name = req.Query["name"];
                if (string.IsNullOrEmpty(name))
                {
                    log.LogError("DeletePipeline: error missing pipeline name");
                    return new BadRequestObjectResult("Error missing pipeline name");
                }
                if (!name.EndsWith(".txt")) name = name + ".txt";
                log.LogInformation($"CreatePipeline: Pipe name {name}");
                DataOpsRepository dops = new DataOpsRepository(storageAccount);
                await dops.DeletePipeline(name);
            }
            catch (Exception ex)
            {
                log.LogError($"DeletePipeline: Error creating pipeline: {ex}");
                return new BadRequestObjectResult($"Error creating pipeline: {ex}");
            }
            string responseMessage = "OK";
            log.LogInformation("DeletePipeline: Completed.");
            return new OkObjectResult(responseMessage);
        }
    }
}
