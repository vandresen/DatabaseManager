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

        public Rules(ILoggerFactory loggerFactory, IConfiguration configuration,
            IDataSourceService ds, IRuleDBAccess ruleDB)
        {
            _logger = loggerFactory.CreateLogger<Rules>();
            _response = new ResponseDto();
            _configuration = configuration;
            _ds = ds;
            _ruleDB = ruleDB;
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
                if (id == null) rules = await _ruleDB.GetRules(connectParameter.ConnectionString);
                else rule = await _ruleDB.GetRule((int)id, connectParameter.ConnectionString);
                _response.Result = rules.ToList();
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
                var response = req.CreateResponse(HttpStatusCode.OK);
                response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

                response.WriteString("Welcome to Azure Functions get prediction sets!");

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
        public HttpResponseData SavePredictionSet(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "PredictionSet")] HttpRequestData req)
        {
            _logger.LogInformation("PredictionSet save: Starting.");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            response.WriteString("Welcome to Azure Functions save!");

            _logger.LogInformation("PredictionSet save: Complete.");
            return response;
        }

        [Function("DeletePredictionSet")]
        public HttpResponseData DeletePredictionSet(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "PredictionSet")] HttpRequestData req)
        {
            _logger.LogInformation("PredictionSet delete: Starting.");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            response.WriteString("Welcome to Azure Functions delete!");

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
                var response = req.CreateResponse(HttpStatusCode.OK);
                response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

                response.WriteString("Welcome to Azure Functions get Function!");

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
        public HttpResponseData SaveFunction(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "Function")] HttpRequestData req)
        {
            _logger.LogInformation("Function save: Starting.");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            response.WriteString("Welcome to Azure Functions save!");

            _logger.LogInformation("Function save: Complete.");
            return response;
        }

        [Function("DeleteFunction")]
        public HttpResponseData DeleteFunction(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "Function")] HttpRequestData req)
        {
            _logger.LogInformation("Function delete: Starting.");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            response.WriteString("Welcome to Azure Functions delete!");

            _logger.LogInformation("Function delete: Complete.");
            return response;
        }

        [Function("GetRuleOptions")]
        public HttpResponseData RuleOptions(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "GetRuleOptions")] HttpRequestData req)
        {
            _logger.LogInformation("GetRuleOptions: Starting.");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            response.WriteString("Welcome to Azure GetRuleOptions!");

            _logger.LogInformation("GetRuleOptions: Complete.");
            return response;
        }
    }
}
