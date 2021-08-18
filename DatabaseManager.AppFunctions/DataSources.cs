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
using System.Linq;
using System.Collections.Generic;
using DatabaseManager.Shared;

namespace DatabaseManager.AppFunctions
{
    public static class DataSources
    {
        [FunctionName("GetDataSources")]
        public static async Task<IActionResult> GetSources(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("GetDataSources: Starting");
            string responseMessage = "";

            try
            {
                var headers = req.Headers;
                string storageAccount = headers.FirstOrDefault(x => x.Key == "azurestorageconnection").Value;
                if (string.IsNullOrEmpty(storageAccount))
                {
                    log.LogError("GetDataSources: error, missing azure storage account");
                    return new BadRequestObjectResult("Missing azure storage account in http header");
                }
                Sources ss = new Sources(storageAccount);
                List<ConnectParameters> connectors = await ss.GetSources();
                responseMessage = JsonConvert.SerializeObject(connectors, Formatting.Indented);
            }
            catch (Exception ex)
            {
                log.LogError("GetDataSources: Error getting data sources: {ex}");
                return new BadRequestObjectResult($"Error getting data sources: {ex}");
            }
            
            log.LogInformation("GetDataSources: Complete");
            return new OkObjectResult(responseMessage);
        }

        [FunctionName("GetDataSource")]
        public static async Task<IActionResult> GetSource(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("GetDataSource: Starting");
            string responseMessage = "";
            try
            {
                
                string name = req.Query["name"];
                if (string.IsNullOrEmpty(name))
                {
                    log.LogError("GetDataSource: error, source name is missing");
                    return new BadRequestObjectResult("Error missing source name");
                }
                var headers = req.Headers;
                string storageAccount = headers.FirstOrDefault(x => x.Key == "azurestorageconnection").Value;
                if (string.IsNullOrEmpty(storageAccount))
                {
                    log.LogError("GetDataSource: error, missing azure storage account");
                    return new BadRequestObjectResult("Missing azure storage account in http header");
                }
                Sources ss = new Sources(storageAccount);
                ConnectParameters connector = await ss.GetSourceParameters(name);
                responseMessage = JsonConvert.SerializeObject(connector, Formatting.Indented);
            }
            catch (Exception ex)
            {
                log.LogError($"GetDataSource: Error getting data source: {ex}");
                return new BadRequestObjectResult($"Error getting data source: {ex}");
            }

            log.LogInformation("GetDataSource: Complete");
            return new OkObjectResult(responseMessage);
        }

        [FunctionName("SaveDataSource")]
        public static async Task<IActionResult> SaveSource(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("SaveDataSource: Starting");
            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                ConnectParameters connector = JsonConvert.DeserializeObject<ConnectParameters>(requestBody);
                if (connector == null)
                {
                    log.LogError("SaveDataSource: error missing source parameters");
                    return new BadRequestObjectResult("Error missing source parameters");
                }
                var headers = req.Headers;
                string storageAccount = headers.FirstOrDefault(x => x.Key == "azurestorageconnection").Value;
                if (string.IsNullOrEmpty(storageAccount))
                {
                    log.LogError("GetDataSource: error, missing azure storage account");
                    return new BadRequestObjectResult("Missing azure storage account in http header");
                }
                Sources ss = new Sources(storageAccount);
                await ss.SaveSource(connector);
            }
            catch (Exception ex)
            {
                log.LogError($"SaveDataSource: Error saving data source: {ex}");
                return new BadRequestObjectResult($"Error saving data source: {ex}");
            }

            log.LogInformation("SaveDataSource: Complete");
            return new OkObjectResult("OK");
        }

        [FunctionName("UpdateDataSource")]
        public static async Task<IActionResult> UpdateSource(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("UpdateDataSource: Starting");
            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                ConnectParameters connector = JsonConvert.DeserializeObject<ConnectParameters>(requestBody);
                if (connector == null)
                {
                    log.LogError("UpdateDataSource: error missing source parameters");
                    return new BadRequestObjectResult("Error missing source parameters");
                }
                var headers = req.Headers;
                string storageAccount = headers.FirstOrDefault(x => x.Key == "azurestorageconnection").Value;
                if (string.IsNullOrEmpty(storageAccount))
                {
                    log.LogError("UpdateDataSource: error, missing azure storage account");
                    return new BadRequestObjectResult("Missing azure storage account in http header");
                }
                Sources ss = new Sources(storageAccount);
                await ss.UpdateSource(connector);
            }
            catch (Exception ex)
            {
                log.LogError($"UpdateDataSource: Error updating data source: {ex}");
                return new BadRequestObjectResult($"Error updating data source: {ex}");
            }

            log.LogInformation("UpdateDataSource: Complete");
            return new OkObjectResult("OK");
        }

        [FunctionName("DeleteDataSource")]
        public static async Task<IActionResult> DeleteSource(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("DeleteDataSource: Starting");
            try
            {
                string name = req.Query["name"];
                if (string.IsNullOrEmpty(name))
                {
                    log.LogError("DeleteDataSource: error missing source name");
                    return new BadRequestObjectResult("Error missing source name");
                }
                var headers = req.Headers;
                string storageAccount = headers.FirstOrDefault(x => x.Key == "azurestorageconnection").Value;
                if (string.IsNullOrEmpty(storageAccount))
                {
                    log.LogError("DeleteDataSource: error, missing azure storage account");
                    return new BadRequestObjectResult("Missing azure storage account in http header");
                }
                Sources ss = new Sources(storageAccount);
                await ss.DeleteSource(name);
            }
            catch (Exception ex)
            {
                log.LogError($"DeleteDataSource: Error deleting data source: {ex}");
                return new BadRequestObjectResult($"Error deleting data source: {ex}");
            }

            log.LogInformation("DeleteDataSource: Complete");
            return new OkObjectResult("OK");
        }
    }
}
