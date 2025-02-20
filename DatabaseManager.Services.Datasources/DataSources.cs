using DatabaseManager.Services.Datasources.Models.Dto;
using DatabaseManager.Services.Datasources.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DatabaseManager.Services.Datasources
{
    public class DataSources
    {
        private readonly ILogger<DataSources> _logger;
        protected ResponseDto _response;
        private readonly IConfiguration _configuration;
        private readonly IDataSourceRepository _dataSourceRepository;

        public DataSources(IDataSourceRepository dataSourceRepository, IConfiguration configuration, ILogger<DataSources> logger)
        {
            _logger = logger;
            _response = new ResponseDto();
            _configuration = configuration;
            _dataSourceRepository = dataSourceRepository;
        }

        [Function("GetDataSources")]
        public async Task<IActionResult> GetSources(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req)
        {
            _logger.LogInformation("GetDataSources: Starting");
            try
            {
                string storageAccount;
                storageAccount = _configuration.GetConnectionString("AzurestorageConnection");
                _logger.LogInformation($"GetDataSources: App settings is {storageAccount}");
                if (string.IsNullOrEmpty(storageAccount))
                {
                    _response.IsSuccess = false;
                    _response.ErrorMessages
                         = new List<string>() { "GetDataSources: Azure connection string is missing" };
                    _logger.LogError("GetDataSources: Azure connection string is missing");
                }
                else
                {
                    List<ConnectParametersDto> sources = await _dataSourceRepository.GetDataSources(storageAccount);
                    _response.Result = sources;
                }
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages
                     = new List<string>() { ex.ToString() };
                _logger.LogError($"GetDataSources: Error getting data sources: {ex}");
            }
            _logger.LogInformation("GetDataSources: Complete");
            return new OkObjectResult(_response);
        }

        [Function("GetDataSource")]
        public async Task<IActionResult> GetSource(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "GetDataSource/{name}")] HttpRequest req, string name)
        {
            _logger.LogInformation("GetDataSource: Starting");
            try
            {
                string storageAccount;
                storageAccount = _configuration.GetConnectionString("AzurestorageConnection");
                _logger.LogInformation($"GetDataSources: App settings is {storageAccount}");
                if (string.IsNullOrEmpty(storageAccount))
                {
                    _response.IsSuccess = false;
                    _response.ErrorMessages
                         = new List<string>() { "GetDataSource: Azure connection string is missing" };
                    _logger.LogError("GetDataSource: Azure connection string is missing");
                }
                else 
                {
                    ConnectParametersDto source = await _dataSourceRepository.GetDataSourceByName(name, storageAccount);
                    _response.Result = source;
                }
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages
                     = new List<string>() { ex.ToString() };
                _logger.LogError($"GetDataSource: Error getting data sources: {ex}");
            }

            _logger.LogInformation("GetDataSource: Complete");
            return new OkObjectResult(_response);
        }

        [Function("SaveDataSource")]
        public async Task<IActionResult> SaveSource(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "SaveDataSource")] HttpRequest req)
        {
            _logger.LogInformation("SaveDataSource: Starting");
            try
            {
                string storageAccount;
                storageAccount = _configuration.GetConnectionString("AzurestorageConnection");
                _logger.LogInformation($"SaveDataSource: App settings is {storageAccount}");
                if (string.IsNullOrEmpty(storageAccount))
                {
                    _response.IsSuccess = false;
                    _response.ErrorMessages
                         = new List<string>() { "SaveDataSource: Azure connection string is missing" };
                    _logger.LogError("SaveDataSource: Azure connection string is missing");
                }
                else
                {
                    string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                    ConnectParametersDto connector = JsonConvert.DeserializeObject<ConnectParametersDto>(requestBody);
                    if (connector == null)
                    {
                        _logger.LogError("SaveDataSource: Error missing connector parameters");
                        _response.IsSuccess = false;
                        _response.ErrorMessages = new List<string>() { "Error missing connector parameters" };
                    }
                    else
                    {
                        ConnectParametersDto source = await _dataSourceRepository.CreateUpdateDataSource(connector, storageAccount);
                        _response.Result = source;
                    }
                }
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages
                     = new List<string>() { ex.ToString() };
                _logger.LogError($"SaveDataSource: Error getting data sources: {ex}");
            }

            _logger.LogInformation("SaveDataSource: Complete");
            return new OkObjectResult(_response);
        }

        [Function("DeleteDataSource")]
        public async Task<IActionResult> DeleteSource(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "DeleteDataSource/{name}")] HttpRequest req, string name)
        {
            _logger.LogInformation("DeleteDataSource: Starting");
            try
            {
                string storageAccount;
                storageAccount = _configuration.GetConnectionString("AzurestorageConnection");
                _logger.LogInformation($"DeleteDataSource: App settings is {storageAccount}");
                if (string.IsNullOrEmpty(storageAccount))
                {
                    _response.IsSuccess = false;
                    _response.ErrorMessages
                         = new List<string>() { "DeleteDataSource: Azure connection string is missing" };
                    _logger.LogError("DeleteDataSource: Azure connection string is missing");
                }
                else
                {
                    bool isSuccess = await _dataSourceRepository.DeleteDataSource(name, storageAccount);
                    _response.Result = isSuccess;
                }
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages
                     = new List<string>() { ex.ToString() };
                _logger.LogError($"DeleteDataSource: Error getting data sources: {ex}");
            }

            _logger.LogInformation("DeleteDataSource: Complete");
            return new OkObjectResult(_response);
        }
    }
}
