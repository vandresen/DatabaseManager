using DatabaseManager.Services.DatabaseManagement.Core;
using DatabaseManager.Services.DatabaseManagement.Extensions;
using DatabaseManager.Services.DatabaseManagement.Models;
using DatabaseManager.Services.DatabaseManagement.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net;
using System.Reflection;

namespace DatabaseManager.Services.DatabaseManagement
{
    public class DatabaseManagement
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        private readonly IDataSourceService _ds;
        private readonly IFileStorageService _embeddedStorage;
        protected ResponseDto _response;

        public DatabaseManagement(ILoggerFactory loggerFactory, IConfiguration configuration,
            IDataSourceService ds)
        {
            _logger = loggerFactory.CreateLogger<DatabaseManagement>();
            _response = new ResponseDto();
            _configuration = configuration;
            _embeddedStorage = new EmbeddedFileStorageService();
            _ds = ds;
            SD.DataSourceAPIBase = _configuration.GetValue<string>("DataSourceAPI");
            SD.DataSourceKey = _configuration["DataSourceKey"];
        }

        [Function("Create")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            _logger.LogInformation("Create: Starting");

            try
            {
                string azureStorageAccount = req.GetStorageKey();
                var stringBody = await new StreamReader(req.Body).ReadToEndAsync();
                DataModelParameters dmParameters = JsonConvert.DeserializeObject<DataModelParameters>(stringBody);
                ResponseDto dsResponse = await _ds.GetDataSourceByNameAsync<ResponseDto>(dmParameters.DataConnector);
                ConnectParameters connectParameter = JsonConvert.DeserializeObject<ConnectParameters>(Convert.ToString(dsResponse.Result));
                DataModelManagement dm = new DataModelManagement(_logger, azureStorageAccount, connectParameter);
                await dm.DataModelCreate(dmParameters);

            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages
                     = new List<string>() { ex.ToString() };
                _logger.LogError($"Create: Error getting data configuration from folder: {ex}");
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(_response);

            _logger.LogInformation("Create: Complete");
            return response;
        }

        [Function("GetDatabaseAccessDefinition")]
        public async Task<HttpResponseData> GetDatabaseAccessDef([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req)
        {
            _logger.LogInformation("GetDatabaseAccessDefinition: Starting");

            try
            {
                string fileName = "PPDMDataAccess.json";
                string definition = await _embeddedStorage.ReadFile("", fileName);
                _response.Result = definition;
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages
                     = new List<string>() { ex.ToString() };
                _logger.LogError($"GetDatabaseAccessDefinition: Error getting data configuration from folder: {ex}");
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(_response);

            _logger.LogInformation("GetDatabaseAccessDefinition: Complete");
            return response;
        }
    }
}
