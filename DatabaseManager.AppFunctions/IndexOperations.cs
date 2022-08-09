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

namespace DatabaseManager.AppFunctions
{
    public static class IndexOperations
    {
        [FunctionName("GetTaxonomies")]
        public static async Task<IActionResult> Taxonomies(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("GetTaxonomies: Starting");
            string responseMessage = "";
            try
            {
                string storageAccount = Common.Helpers.Common.GetStorageKey(req);
                IndexManagement im = new IndexManagement(storageAccount);
                responseMessage = await im.GetTaxonomies();
            }
            catch (Exception ex)
            {
                log.LogError($"GetTaxonomies: {ex}");
                return new BadRequestObjectResult($"Error getting taxonomies: {ex}");
            }

            log.LogInformation("GetTaxonomies: Complete");
            return new OkObjectResult(responseMessage);
        }

        [FunctionName("GetIndexData")]
        public static async Task<IActionResult> IndexData(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("GetIndexData: Starting");
            string responseMessage = "";
            try
            {
                string name = req.Query["name"];
                if (string.IsNullOrEmpty(name))
                {
                    log.LogError("GetIndexData: error, source name is missing");
                    return new BadRequestObjectResult("Error missing source name");
                }
                string storageAccount = Common.Helpers.Common.GetStorageKey(req);
                IndexManagement im = new IndexManagement(storageAccount);
                responseMessage = await im.GetIndexData(name);
            }
            catch (Exception ex)
            {
                log.LogError($"GetIndexData: {ex}");
                return new BadRequestObjectResult($"Error getting index data: {ex}");
            }

            log.LogInformation("GetIndexData: Complete");
            return new OkObjectResult(responseMessage);
        }

        [FunctionName("GetIndexItem")]
        public static async Task<IActionResult> IndexItem(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("GetIndexItem: Starting");
            string responseMessage = "";
            try
            {
                string name = req.Query["name"];
                if (string.IsNullOrEmpty(name))
                {
                    log.LogError("GetIndexItem: error, source name is missing");
                    return new BadRequestObjectResult("Error missing source name");
                }
                int id = Common.Helpers.Common.GetIntFromWebQuery(req, "id");
                string storageAccount = Common.Helpers.Common.GetStorageKey(req);
                IndexManagement im = new IndexManagement(storageAccount);
                responseMessage = await im.GetIndexItem(name, id);
            }
            catch (Exception ex)
            {
                log.LogError($"GetIndexItem: {ex}");
                return new BadRequestObjectResult($"Error getting index item: {ex}");
            }

            log.LogInformation("GetIndexItem: Complete");
            return new OkObjectResult(responseMessage);
        }

        [FunctionName("CreateIndex")]
        public static async Task<IActionResult> MakeIndex(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("CreateIndex: Starting");
            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                CreateIndexParameters indexParm = JsonConvert.DeserializeObject<CreateIndexParameters>(requestBody);
                if (indexParm == null)
                {
                    log.LogError("CreateIndex: error missing index parameters");
                    return new BadRequestObjectResult("Error missing index parameters");
                }
                string storageAccount = Common.Helpers.Common.GetStorageKey(req);
                IndexManagement im = new IndexManagement(storageAccount);
                await im.CreateIndex(indexParm);
            }
            catch (Exception ex)
            {
                log.LogError($"CreateIndex: {ex}");
                return new BadRequestObjectResult($"Error creating index: {ex}");
            }

            log.LogInformation("CreateIndex: Complete");
            return new OkObjectResult("OK");
        }

        [FunctionName("GetIndexTaxonomy")]
        public static async Task<IActionResult> IndexRootTaxonomy(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("GetIndexTaxonomy: Starting");
            string responseMessage = "";
            try
            {
                string name = req.Query["name"];
                if (string.IsNullOrEmpty(name))
                {
                    log.LogError("GetIndexTaxonomy: error, source name is missing");
                    return new BadRequestObjectResult("Error missing source name");
                }
                string storageAccount = Common.Helpers.Common.GetStorageKey(req);
                IndexManagement im = new IndexManagement(storageAccount);
                responseMessage = await im.GetIndexTaxonomy(name);
            }
            catch (Exception ex)
            {
                log.LogError($"GetIndexTaxonomy: {ex}");
                return new BadRequestObjectResult($"Error getting index data: {ex}");
            }

            log.LogInformation("GetIndexTaxonomy: Complete");
            return new OkObjectResult(responseMessage);
        }

        [FunctionName("GetSingleIndexItem")]
        public static async Task<IActionResult> SingleIndexItem(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("GetSingleIndexItem: Starting");
            string responseMessage = "";
            try
            {
                string name = req.Query["name"];
                if (string.IsNullOrEmpty(name))
                {
                    log.LogError("GetSingleIndexItem: error, source name is missing");
                    return new BadRequestObjectResult("Error missing source name");
                }
                int id = Common.Helpers.Common.GetIntFromWebQuery(req, "id");
                string storageAccount = Common.Helpers.Common.GetStorageKey(req);
                IndexManagement im = new IndexManagement(storageAccount);
                responseMessage = await im.GetSingleIndexItem(name, id);
            }
            catch (Exception ex)
            {
                log.LogError($"GetSingleIndexItem: {ex}");
                return new BadRequestObjectResult($"Error getting index data: {ex}");
            }

            log.LogInformation("GetSingleIndexItem: Complete");
            return new OkObjectResult(responseMessage);
        }
    }
}