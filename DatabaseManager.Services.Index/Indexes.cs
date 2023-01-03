using System.Net;
using DatabaseManager.Services.Index.Helpers;
using DatabaseManager.Services.Index.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DatabaseManager.Services.Index
{
    public class Indexes
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        protected ResponseDto _response;

        public Indexes(ILoggerFactory loggerFactory, IConfiguration configuration)
        {
            _logger = loggerFactory.CreateLogger<Indexes>();
            this._response = new ResponseDto();
            _configuration = configuration;
            SD.DataSourceAPIBase = _configuration.GetValue<string>("DataSourceAPI");
        }

        [Function("GetIndexes")]
        public ResponseDto GetIndexes(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "Indexes")] HttpRequestData req)
        {
            _logger.LogInformation("GetIndexes: Starting.");

            try
            {
                var settingOne = _configuration.GetValue<string>("DataSourceAPI");
                _logger.LogInformation($"GetIndexes: setting is {SD.DataSourceAPIBase}");
                IndexData idx = new IndexData(_logger);
                List<IndexDto> indexes = idx.GetIndexes(req);
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

        [Function("GetIndex")]
        public HttpResponseData GetIndex(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "Indexes/{id}")] HttpRequestData req,
            int id)
        {
            _logger.LogInformation("GetIndex: Starting.");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            response.WriteString("Welcome to Azure Functions!");

            return response;
        }

        [Function("SaveIndexes")]
        public HttpResponseData SaveIndexes(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "Indexes")] HttpRequestData req)
        {
            _logger.LogInformation("SaveIndexes: Starting.");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            response.WriteString("Welcome to Azure Functions!");

            return response;
        }

        [Function("SaveIndex")]
        public HttpResponseData SaveIndex(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "Indexes")] HttpRequestData req)
        {
            _logger.LogInformation("SaveIndex: Starting");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            response.WriteString("Welcome to Azure Functions!");

            return response;
        }

        [Function("DeleteIndex")]
        public HttpResponseData DeleteIndex(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "Indexes/{id}")] HttpRequestData req,
            int id)
        {
            _logger.LogInformation("DeleteIndex: Starting");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            response.WriteString($"Welcome to Azure Functions, id = {id}!");

            return response;
        }
    }
}
