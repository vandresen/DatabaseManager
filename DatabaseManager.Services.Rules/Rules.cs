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
                _logger.LogError($"GetIndexes: Error getting indexes: {ex}");
            }
            return _response;
        }

        [Function("SaveRules")]
        public HttpResponseData SaveRules(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "Rules")] HttpRequestData req)
        {
            _logger.LogInformation("Rules save: Starting.");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            response.WriteString("Welcome to Azure Functions save!");

            _logger.LogInformation("Rules save: Complete.");
            return response;
        }

        [Function("DeleteRules")]
        public HttpResponseData DeleteRules(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "Rules")] HttpRequestData req)
        {
            _logger.LogInformation("Rules delete: Starting.");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            response.WriteString("Welcome to Azure Functions delete!");

            _logger.LogInformation("Rules delete: Complete.");
            return response;
        }
    }
}