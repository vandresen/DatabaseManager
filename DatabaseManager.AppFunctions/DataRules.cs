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
using DatabaseManager.Common.Extensions;
using DatabaseManager.Shared;

namespace DatabaseManager.AppFunctions
{
    public static class DataRules
    {
        [FunctionName("GetDataRules")]
        public static async Task<IActionResult> GetRules(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("GetDataRules: Starting");
            string responseMessage = "";

            try
            {
                string name = req.Query["name"];
                if (string.IsNullOrEmpty(name))
                {
                    log.LogError("GetDataRules: error, source name is missing");
                    return new BadRequestObjectResult("Error missing source name");
                }
                var headers = req.Headers;
                string storageAccount = headers.FirstOrDefault(x => x.Key == "azurestorageconnection").Value;
                if (string.IsNullOrEmpty(storageAccount))
                {
                    log.LogError("GetDataRules: error, missing azure storage account");
                    return new BadRequestObjectResult("Missing azure storage account in http header");
                }
                RuleManagement rules = new RuleManagement(storageAccount);
                responseMessage = await rules.GetRules(name);
            }
            catch (Exception ex)
            {
                log.LogError($"GetDataRules: Error getting rules: {ex}");
                return new BadRequestObjectResult($"Error getting rules: {ex}");
            }

            return new OkObjectResult(responseMessage);
        }

        [FunctionName("GetDataRule")]
        public static async Task<IActionResult> GetRule(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("GetDataRule: Starting");
            string responseMessage = "";

            try
            {
                string name = req.Query["name"];
                if (string.IsNullOrEmpty(name))
                {
                    log.LogError("GetDataRule: error, source name is missing");
                    return new BadRequestObjectResult("Error missing source name");
                }
                
                string strId = req.Query["id"];
                int? tmpId = strId.GetIntFromString();
                if (tmpId == null)
                {
                    log.LogError("GetDataRule: error, rule id name is missing");
                    return new BadRequestObjectResult("Error missing rule id");
                }
                int id = tmpId.GetValueOrDefault();

                var headers = req.Headers;
                string storageAccount = headers.FirstOrDefault(x => x.Key == "azurestorageconnection").Value;
                if (string.IsNullOrEmpty(storageAccount))
                {
                    log.LogError("GetDataRules: error, missing azure storage account");
                    return new BadRequestObjectResult("Missing azure storage account in http header");
                }
                RuleManagement rules = new RuleManagement(storageAccount);
                responseMessage = await rules.GetRule(name, id);
            }
            catch (Exception ex)
            {
                log.LogError($"GetDataRule: Error getting rules: {ex}");
                return new BadRequestObjectResult($"Error getting rules: {ex}");
            }

            return new OkObjectResult(responseMessage);
        }

        [FunctionName("GetPredictionSets")]
        public static async Task<IActionResult> GetPredictions(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("GetPredictionSets: Starting");
            string responseMessage = "";

            try
            {
                var headers = req.Headers;
                string storageAccount = headers.FirstOrDefault(x => x.Key == "azurestorageconnection").Value;
                if (string.IsNullOrEmpty(storageAccount))
                {
                    log.LogError("GetPredictionSets: error, missing azure storage account");
                    return new BadRequestObjectResult("Missing azure storage account in http header");
                }
                RuleManagement rules = new RuleManagement(storageAccount);
                responseMessage = await rules.GetPredictions();
            }
            catch (Exception ex)
            {
                log.LogError($"GetPredictionSets: Error getting rules: {ex}");
                return new BadRequestObjectResult($"Error getting rules: {ex}");
            }

            return new OkObjectResult(responseMessage);
        }

        [FunctionName("GetPredictionSet")]
        public static async Task<IActionResult> GetPrediction(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("GetPredictionSet: Starting");
            string responseMessage = "";

            try
            {
                string name = req.Query["name"];
                if (string.IsNullOrEmpty(name))
                {
                    log.LogError("GetPredictionSet: error, prediction name is missing");
                    return new BadRequestObjectResult("Error missing prediction name");
                }
                var headers = req.Headers;
                string storageAccount = headers.FirstOrDefault(x => x.Key == "azurestorageconnection").Value;
                if (string.IsNullOrEmpty(storageAccount))
                {
                    log.LogError("GetPredictionSet: error, missing azure storage account");
                    return new BadRequestObjectResult("Missing azure storage account in http header");
                }
                RuleManagement rules = new RuleManagement(storageAccount);
                responseMessage = await rules.GetPrediction(name);
            }
            catch (Exception ex)
            {
                log.LogError($"GetPredictionSet: Error getting rules: {ex}");
                return new BadRequestObjectResult($"Error getting rules: {ex}");
            }

            return new OkObjectResult(responseMessage);
        }

        [FunctionName("GetRuleInfo")]
        public static async Task<IActionResult> RuleInfo(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("GetRuleInfo: Starting");
            string responseMessage = "";

            try
            {
                var headers = req.Headers;
                string storageAccount = headers.FirstOrDefault(x => x.Key == "azurestorageconnection").Value;
                if (string.IsNullOrEmpty(storageAccount))
                {
                    log.LogError("GetRuleInfo: error, missing azure storage account");
                    return new BadRequestObjectResult("Missing azure storage account in http header");
                }
                RuleManagement rules = new RuleManagement(storageAccount);
                responseMessage = await rules.GetRuleInfo();
            }
            catch (Exception ex)
            {
                log.LogError($"GetRuleInfo: Error getting rule info: {ex}");
                return new BadRequestObjectResult($"Error getting rule info: {ex}");
            }

            return new OkObjectResult(responseMessage);
        }

        [FunctionName("SaveDataRule")]
        public static async Task<IActionResult> SaveRule(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("SaveDataRule: Starting");

            try
            {
                string name = req.Query["name"];
                if (string.IsNullOrEmpty(name))
                {
                    log.LogError("SaveDataRule: error, source name is missing");
                    return new BadRequestObjectResult("Error missing source name");
                }
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                RuleModel rule = JsonConvert.DeserializeObject<RuleModel>(requestBody);
                if (rule == null)
                {
                    log.LogError("SaveDataRule: error missing rule");
                    return new BadRequestObjectResult("Error missing rule");
                }
                var headers = req.Headers;
                string storageAccount = headers.FirstOrDefault(x => x.Key == "azurestorageconnection").Value;
                if (string.IsNullOrEmpty(storageAccount))
                {
                    log.LogError("SaveDataRule: error, missing azure storage account");
                    return new BadRequestObjectResult("Missing azure storage account in http header");
                }
                RuleManagement rules = new RuleManagement(storageAccount);
                await rules.SaveRule(name, rule);
            }
            catch (Exception ex)
            {
                log.LogError($"SaveDataRule: Error saving rule: {ex}");
                return new BadRequestObjectResult($"Error saving rule: {ex}");
            }

            return new OkObjectResult("OK");
        }

        [FunctionName("SavePredictionSet")]
        public static async Task<IActionResult> SavePrediction(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("SavePredictionSet: Starting");

            try
            {
                string name = req.Query["name"];
                if (string.IsNullOrEmpty(name))
                {
                    log.LogError("SaveDaSavePredictionSettaRule: error, prediction set name is missing");
                    return new BadRequestObjectResult("Error missing prediction set name");
                }
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                PredictionSet set = JsonConvert.DeserializeObject<PredictionSet>(requestBody);
                if (set == null || set.RuleSet == null)
                {
                    log.LogError("SavePredictionSet: error missing prediction set");
                    return new BadRequestObjectResult("Error missing prediction set");
                }
                var headers = req.Headers;
                string storageAccount = headers.FirstOrDefault(x => x.Key == "azurestorageconnection").Value;
                if (string.IsNullOrEmpty(storageAccount))
                {
                    log.LogError("SavePredictionSet: error, missing azure storage account");
                    return new BadRequestObjectResult("Missing azure storage account in http header");
                }
                RuleManagement rules = new RuleManagement(storageAccount);
                await rules.SavePredictionSet(name, set);
            }
            catch (Exception ex)
            {
                log.LogError($"SavePredictionSet: Error saving prediction set: {ex}");
                return new BadRequestObjectResult($"Error saving prediction set: {ex}");
            }

            return new OkObjectResult("OK");
        }

        [FunctionName("UpdateDataRule")]
        public static async Task<IActionResult> UpdateRule(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("UpdateDataRule: Starting");

            try
            {
                string name = req.Query["name"];
                if (string.IsNullOrEmpty(name))
                {
                    log.LogError("UpdateDataRule: error, source name is missing");
                    return new BadRequestObjectResult("Error missing source name");
                }

                string strId = req.Query["id"];
                int? tmpId = strId.GetIntFromString();
                if (tmpId == null)
                {
                    log.LogError("UpdateDataRule: error, rule id name is missing");
                    return new BadRequestObjectResult("Error missing rule id");
                }
                int id = tmpId.GetValueOrDefault();

                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                RuleModel rule = JsonConvert.DeserializeObject<RuleModel>(requestBody);
                if (rule == null)
                {
                    log.LogError("UpdateDataRule: error missing rule data");
                    return new BadRequestObjectResult("Error missing rule data");
                }
                var headers = req.Headers;
                string storageAccount = headers.FirstOrDefault(x => x.Key == "azurestorageconnection").Value;
                if (string.IsNullOrEmpty(storageAccount))
                {
                    log.LogError("SavePredictionSet: error, missing azure storage account");
                    return new BadRequestObjectResult("Missing azure storage account in http header");
                }
                RuleManagement rules = new RuleManagement(storageAccount);
                await rules.UpdateRule(name, id, rule);
            }
            catch (Exception ex)
            {
                log.LogError($"UpdateDataRule: Error updating rule: {ex}");
                return new BadRequestObjectResult($"Error updating rule: {ex}");
            }

            return new OkObjectResult("OK");
        }

        [FunctionName("DeleteDataRule")]
        public static async Task<IActionResult> DeleteRule(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("DeleteDataRule: Starting");
            try
            {
                string name = req.Query["name"];
                if (string.IsNullOrEmpty(name))
                {
                    log.LogError("DeleteDataRule: error, source name is missing");
                    return new BadRequestObjectResult("Error missing source name");
                }

                string strId = req.Query["id"];
                int? tmpId = strId.GetIntFromString();
                if (tmpId == null)
                {
                    log.LogError("DeleteDataRule: error, rule id name is missing");
                    return new BadRequestObjectResult("Error missing rule id");
                }
                int id = tmpId.GetValueOrDefault();

                string storageAccount = Common.Helpers.Common.GetStorageKey(req);
                RuleManagement rules = new RuleManagement(storageAccount);
                await rules.DeleteRule(name, id);
            }
            catch (Exception ex)
            {
                log.LogError($"DeleteDataRule: {ex}");
                return new BadRequestObjectResult($"Error deleting rule: {ex}");
            }

            log.LogInformation("DeleteDataRule: Complete");
            return new OkObjectResult("OK");
        }

        [FunctionName("DeletePredictionSet")]
        public static async Task<IActionResult> DeletePrediction(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("DeletePredictionSet: Starting");
            try
            {
                string name = req.Query["name"];
                if (string.IsNullOrEmpty(name))
                {
                    log.LogError("DeletePredictionSet: error, prediction set name is missing");
                    return new BadRequestObjectResult("Error missing prediction set name");
                }
                string storageAccount = Common.Helpers.Common.GetStorageKey(req);
                RuleManagement rules = new RuleManagement(storageAccount);
                await rules.DeletePrediction(name);
            }
            catch (Exception ex)
            {
                log.LogError($"DeletePredictionSet: {ex}");
                return new BadRequestObjectResult($"Error deleting rule: {ex}");
            }

            log.LogInformation("DeletePredictionSet: Complete");
            return new OkObjectResult("OK");
        }
    }
}
