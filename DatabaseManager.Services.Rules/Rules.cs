using System.Data;
using System.Net;
using Azure;
using DatabaseManager.Services.Rules.Extensions;
using DatabaseManager.Services.Rules.Models;
using DatabaseManager.Services.Rules.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DatabaseManager.Services.Rules
{
    public class Rules
    {
        private readonly ILogger _logger;
        protected ResponseDto _response;
        private readonly IConfiguration _configuration;
        private readonly IDataSourceService _ds;
        private readonly IRuleDBAccess _ruleDB;
        private readonly IPredictionSetAccess _prediction;
        private readonly IFunctionAccess _function;

        public Rules(ILoggerFactory loggerFactory, IConfiguration configuration,
            IDataSourceService ds, IRuleDBAccess ruleDB, IPredictionSetAccess prediction,
            IFunctionAccess function)
        {
            _logger = loggerFactory.CreateLogger<Rules>();
            _response = new ResponseDto();
            _configuration = configuration;
            _ds = ds;
            _ruleDB = ruleDB;
            _prediction = prediction;
            _function = function;
            SD.DataSourceAPIBase = _configuration.GetValue<string>("DataSourceAPI");
            SD.DataSourceKey = _configuration["DataSourceKey"];
        }

        [Function("GetRules")]
        public async Task<ResponseDto> GetRules(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "Rules")] HttpRequestData req)
        {
            _logger.LogInformation("Rules get: Starting.");

            try
            {
                string name = req.GetQuery("Name", true);
                int? id = req.GetQuery("Id", false).GetIntFromString();
                ResponseDto dsResponse = await _ds.GetDataSourceByNameAsync<ResponseDto>(name);
                _logger.LogInformation("Rules get: Successfully got the connect info.");
                IEnumerable<RuleModelDto> rules = Enumerable.Empty<RuleModelDto>();
                RuleModelDto rule = new RuleModelDto();
                ConnectParametersDto connectParameter = JsonConvert.DeserializeObject<ConnectParametersDto>(Convert.ToString(dsResponse.Result));
                if (id == null)
                {
                    rules = await _ruleDB.GetRules(connectParameter.ConnectionString);
                    _response.Result = rules.ToList();
                }
                else 
                { 
                    rule = await _ruleDB.GetRule((int)id, connectParameter.ConnectionString);
                    _response.Result = rule;
                }
                
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages
                     = new List<string>() { ex.ToString() };
                _logger.LogError($"Rules get: Error getting rules: {ex}");
            }
            return _response;
        }

        [Function("GetRule")]
        public async Task<ResponseDto> GetRule(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "Rule")] HttpRequestData req)
        {
            _logger.LogInformation("Rule get: Starting.");

            try
            {
                string name = req.GetQuery("Name", true);
                int? id = req.GetQuery("Id", true).GetIntFromString();
                ResponseDto dsResponse = await _ds.GetDataSourceByNameAsync<ResponseDto>(name);
                _logger.LogInformation("Rules get: Successfully got the connect info.");
                IEnumerable<RuleModelDto> rules = Enumerable.Empty<RuleModelDto>();
                RuleModelDto rule = new RuleModelDto();
                ConnectParametersDto connectParameter = JsonConvert.DeserializeObject<ConnectParametersDto>(Convert.ToString(dsResponse.Result));
                rule = await _ruleDB.GetRule((int)id, connectParameter.ConnectionString);
                _response.Result = rule;
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages
                     = new List<string>() { ex.ToString() };
                _logger.LogError($"Rules get: Error getting rules: {ex}");
            }
            return _response;
        }

        [Function("SaveRules")]
        public async Task<HttpResponseData> SaveRules(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "Rules")] HttpRequestData req)
        {
            _logger.LogInformation("Rules save: Starting.");

            try
            {
                string name = req.GetQuery("Name", true);
                ResponseDto dsResponse = await _ds.GetDataSourceByNameAsync<ResponseDto>(name);
                ConnectParametersDto connectParameter = JsonConvert.DeserializeObject<ConnectParametersDto>(Convert.ToString(dsResponse.Result));
                string connectionString = connectParameter.CreateDatabaseConnectionString();
                if (connectParameter.SourceType != "DataBase")
                {
                    Exception error = new Exception($"Rules: data source must be a Database type");
                    throw error;
                }

                var stringBody = await new StreamReader(req.Body).ReadToEndAsync();
                RuleModelDto rule = JsonConvert.DeserializeObject<RuleModelDto>(Convert.ToString(stringBody));
                
                await _ruleDB.CreateUpdateRule(rule, connectParameter.ConnectionString);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages
                     = new List<string>() { ex.ToString() };
                _logger.LogError($"Rules: Error saving rule: {ex}");
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(_response);

            _logger.LogInformation("Rules save: Complete.");
            return response;
        }

        [Function("DeleteRules")]
        public async Task<HttpResponseData> DeleteRules(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "Rules")] HttpRequestData req)
        {
            _logger.LogInformation("Rules delete: Starting.");
            
            try
            {
                string name = req.GetQuery("Name", true);
                int id = (int)req.GetQuery("Id", true).GetIntFromString();
                ResponseDto dsResponse = await _ds.GetDataSourceByNameAsync<ResponseDto>(name);
                ConnectParametersDto connectParameter = JsonConvert.DeserializeObject<ConnectParametersDto>(Convert.ToString(dsResponse.Result));
                string connectionString = connectParameter.CreateDatabaseConnectionString();
                await _ruleDB.DeleteRule(id, connectionString);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages
                     = new List<string>() { ex.ToString() };
                _logger.LogError($"Rules: Error deleting rule: {ex}");
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(_response);

            _logger.LogInformation("Rules delete: Complete.");
            return response;
        }

        [Function("GetPredictionSet")]
        public async Task<ResponseDto> GetPredictions(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "PredictionSet")] HttpRequestData req)
        {
            _logger.LogInformation("PredictionSets get: Starting.");

            try
            {
                string azureStorageAccount = req.GetStorageKey();
                string name = req.GetQuery("Name", false);
                if(name == null)
                {
                    List<PredictionSet> predictionSets = _prediction.GetPredictionDataSets(azureStorageAccount);
                    _response.Result = predictionSets;
                }
                else
                {
                    PredictionSet predictionSet = _prediction.GetPredictionDataSet(name, azureStorageAccount);
                    _response.Result = predictionSet;
                }

                _logger.LogInformation("PredictionSets get: Complete.");
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages
                     = new List<string>() { ex.ToString() };
                _logger.LogError($"PredictionSets: Error getting prediction sets: {ex}");
            }
            return _response;
        }

        [Function("SavePredictionSet")]
        public async Task<HttpResponseData> SavePredictionSet(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "PredictionSet")] HttpRequestData req)
        {
            _logger.LogInformation("PredictionSet save: Starting.");

            try
            {
                string azureStorageAccount = req.GetStorageKey();
                var stringBody = await new StreamReader(req.Body).ReadToEndAsync();
                if (String.IsNullOrEmpty(stringBody))
                {
                    Exception error = new Exception($"PredictionSet save: Body is missing");
                    throw error;
                }
                PredictionSet predictionSet = JsonConvert.DeserializeObject<PredictionSet>(Convert.ToString(stringBody));
                List<PredictionSet> existingPredictionSets = _prediction.GetPredictionDataSets(azureStorageAccount);
                var existingPredictionSet = existingPredictionSets.FirstOrDefault(m => m.Name == predictionSet.Name);
                if (existingPredictionSet == null)
                {
                    await _prediction.SavePredictionDataSet(predictionSet, azureStorageAccount);
                }
                else
                {
                    _prediction.UpdatePredictionDataSet(predictionSet, azureStorageAccount);
                }
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages
                     = new List<string>() { ex.ToString() };
                _logger.LogError($"Rules: Error saving prediction set: {ex}");
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(_response);

            _logger.LogInformation("PredictionSet save: Complete.");
            return response;
        }

        [Function("DeletePredictionSet")]
        public async Task<HttpResponseData> DeletePredictionSet(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "PredictionSet")] HttpRequestData req)
        {
            _logger.LogInformation("PredictionSet delete: Starting.");

            try
            {
                string azureStorageAccount = req.GetStorageKey();
                string name = req.GetQuery("Name", true);
                _prediction.DeletePredictionDataSet(name, azureStorageAccount);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages
                     = new List<string>() { ex.ToString() };
                _logger.LogError($"Rules: Error saving prediction set: {ex}");
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(_response);

            _logger.LogInformation("PredictionSet delete: Complete.");
            return response;
        }

        [Function("GetFunction")]
        public async Task<ResponseDto> GetFunction(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "Function")] HttpRequestData req)
        {
            _logger.LogInformation("Function get: Starting.");

            try
            {
                string name = req.GetQuery("Name", true);
                int? id = req.GetQuery("Id", false).GetIntFromString();
                ResponseDto dsResponse = await _ds.GetDataSourceByNameAsync<ResponseDto>(name);
                ConnectParametersDto connectParameter = JsonConvert.DeserializeObject<ConnectParametersDto>(Convert.ToString(dsResponse.Result));
                if (id == null)
                {
                    IEnumerable<RuleFunctionsDto> ruleFunctions = await _function.GetFunctions(connectParameter.ConnectionString);
                    _response.Result = ruleFunctions.ToList();
                }
                else
                {
                    RuleFunctionsDto ruleFunction = await _function.GetFunction((int)id, connectParameter.ConnectionString);
                    _response.Result = ruleFunction;
                }
                _logger.LogInformation("Function get: Complete.");
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages
                     = new List<string>() { ex.ToString() };
                _logger.LogError($"Function: Error getting Function: {ex}");
            }
            return _response;
        }

        [Function("SaveFunction")]
        public async Task<HttpResponseData> SaveFunction(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "Function")] HttpRequestData req)
        {
            _logger.LogInformation("Function save: Starting.");
            try
            {
                string name = req.GetQuery("Name", true);
                ResponseDto dsResponse = await _ds.GetDataSourceByNameAsync<ResponseDto>(name);
                ConnectParametersDto connectParameter = JsonConvert.DeserializeObject<ConnectParametersDto>(Convert.ToString(dsResponse.Result));
                string connectionString = connectParameter.CreateDatabaseConnectionString();
                if (connectParameter.SourceType != "DataBase")
                {
                    Exception error = new Exception($"Rules: data source must be a Database type");
                    throw error;
                }

                var stringBody = await new StreamReader(req.Body).ReadToEndAsync();
                if (String.IsNullOrEmpty(stringBody))
                {
                    Exception error = new Exception($"PredictionSet save: Body is missing");
                    throw error;
                }
                RuleFunctionsDto ruleFunction = JsonConvert.DeserializeObject<RuleFunctionsDto>(Convert.ToString(stringBody));

                await _function.CreateUpdateFunction(ruleFunction, connectionString);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages
                     = new List<string>() { ex.ToString() };
                _logger.LogError($"Rules: Error saving function: {ex}");
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(_response);

            _logger.LogInformation("Function save: Complete.");
            return response;
        }

        [Function("DeleteFunction")]
        public async Task<HttpResponseData> DeleteFunction(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "Function")] HttpRequestData req)
        {
            _logger.LogInformation("Function delete: Starting.");
            try
            {
                string name = req.GetQuery("Name", true);
                int id = (int)req.GetQuery("Id", true).GetIntFromString();
                ResponseDto dsResponse = await _ds.GetDataSourceByNameAsync<ResponseDto>(name);
                ConnectParametersDto connectParameter = JsonConvert.DeserializeObject<ConnectParametersDto>(Convert.ToString(dsResponse.Result));
                string connectionString = connectParameter.CreateDatabaseConnectionString();
                await _function.DeleteFunction(id, connectionString);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages
                     = new List<string>() { ex.ToString() };
                _logger.LogError($"Rules: Error deleting function: {ex}");
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(_response);

            _logger.LogInformation("Function delete: Complete.");
            return response;
        }
    }
}
