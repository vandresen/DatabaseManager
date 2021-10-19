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
    public static class DataTransferOperations
    {
        [FunctionName("GetDataObjects")]
        public static async Task<IActionResult> DataObjects(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("GetDataObjects: Starting");
            string responseMessage = "";

            try
            {
                string name = req.Query["name"];
                if (string.IsNullOrEmpty(name))
                {
                    log.LogError("GetDataRules: error, source name is missing");
                    return new BadRequestObjectResult("Error missing source name");
                }
                string storageAccount = Common.Helpers.Common.GetStorageKey(req);
                DataTransfer dt = new DataTransfer(storageAccount);
                List<string> files = await dt.GetFiles(name);
                List<TransferParameters> transParm = new List<TransferParameters>();
                foreach (var file in files)
                {
                    transParm.Add(new TransferParameters { TargetName = name, Table = file });
                }
                responseMessage = JsonConvert.SerializeObject(transParm, Formatting.Indented);
            }
            catch (Exception ex)
            {
                log.LogError($"GetDataRules: Error getting rules: {ex}");
                return new BadRequestObjectResult($"Error getting rules: {ex}");
            }
            log.LogInformation("GetDataObjects: Complete");
            return new OkObjectResult(responseMessage);
        }

        [FunctionName("GetTransferQueueMessage")]
        public static async Task<IActionResult> GetQueueMessage(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("GetTransferQueueMessage: Starting");
            string responseMessage = "";

            try
            {
                string storageAccount = Common.Helpers.Common.GetStorageKey(req);
                DataTransfer dt = new DataTransfer(storageAccount);
                List<MessageQueueInfo> messages = dt.GetQueueMessage();
                responseMessage = JsonConvert.SerializeObject(messages, Formatting.Indented);
            }
            catch (Exception ex)
            {
                log.LogError($"GetDataRules: Error getting rules: {ex}");
                return new BadRequestObjectResult($"Error getting rules: {ex}");
            }
            log.LogInformation("GetTransferQueueMessage: Complete");
            return new OkObjectResult(responseMessage);
        }

        [FunctionName("TransferData")]
        public static async Task<IActionResult> CopyData(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("TransferData: Starting");
            try
            {
                string storageAccount = Common.Helpers.Common.GetStorageKey(req);
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                TransferParameters transParm = JsonConvert.DeserializeObject<TransferParameters>(requestBody);
                if (transParm == null)
                {
                    log.LogError("TransferData: error missing transfer parameters");
                    return new BadRequestObjectResult("Error missing transfer parameters");
                }
                DataTransfer dt = new DataTransfer(storageAccount);
                await dt.CopyFiles(transParm);
            }
            catch (Exception ex)
            {
                log.LogError($"TransferData: Error getting rules: {ex}");
                return new BadRequestObjectResult($"Error getting rules: {ex}");
            }
            log.LogInformation("TransferData: Complete");
            return new OkObjectResult("OK");
        }

        [FunctionName("TransferRemote")]
        public static async Task<IActionResult> CopyRemote(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("TransferRemote: Starting");
            try
            {
                string storageAccount = Common.Helpers.Common.GetStorageKey(req);
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                TransferParameters transParm = JsonConvert.DeserializeObject<TransferParameters>(requestBody);
                if (transParm == null)
                {
                    log.LogError("TransferRemote: error missing transfer parameters");
                    return new BadRequestObjectResult("Error missing transfer parameters");
                }
                DataTransfer dt = new DataTransfer(storageAccount);
                dt.CopyRemote(transParm);
            }
            catch (Exception ex)
            {
                log.LogError($"TransferRemote: Error getting rules: {ex}");
                return new BadRequestObjectResult($"Error getting rules: {ex}");
            }
            log.LogInformation("TransferRemote: Complete");
            return new OkObjectResult("OK");
        }

        [FunctionName("DeleteTable")]
        public static async Task<IActionResult> TableDelete(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("DeleteTable: Starting");
            try
            {
                string storageAccount = Common.Helpers.Common.GetStorageKey(req);
                string target = Common.Helpers.Common.GetQueryString(req, "name");
                string table = Common.Helpers.Common.GetQueryString(req, "table");
                DataTransfer dt = new DataTransfer(storageAccount);
                await dt.DeleteTable(target, table);
            }
            catch (Exception ex)
            {
                log.LogError($"DeleteTable: Error getting rules: {ex}");
                return new BadRequestObjectResult($"Error getting rules: {ex}");
            }
            log.LogInformation("DeleteTable: Complete");
            return new OkObjectResult("OK");
        }
    }

}
