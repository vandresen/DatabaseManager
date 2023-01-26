using System.Collections.Generic;
using System.Net;
using DatabaseManager.Services.DataConfiguration.Extensions;
using DatabaseManager.Services.DataConfiguration.Models;
using DatabaseManager.Services.DataConfiguration.Repository;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace DatabaseManager.Services.DataConfiguration
{
    public class DataConfiguration
    {
        private readonly ILogger _logger;
        private readonly IDataRepository _dataRepository;
        private ResponseDto _response;

        public DataConfiguration(ILoggerFactory loggerFactory, IDataRepository dataRepository)
        {
            _logger = loggerFactory.CreateLogger<DataConfiguration>();
            this._response = new ResponseDto();
            this._dataRepository = dataRepository;
        }

        [Function("GetDataConfiguration")]
        public async Task<ResponseDto> GetConfiguration([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req)
        {
            _logger.LogInformation("GetDataConfiguration: Starting");

            try
            {
                string storageAccount = req.GetStorageKey();
                string folder = req.GetQuery("folder", true);
                string name = req.GetQuery("Name", false);
                _dataRepository.SetConnectionString(storageAccount);
                if (string.IsNullOrEmpty(name)) 
                {
                    _logger.LogInformation("GetDataConfiguration: Get list of files");
                    List<string> list = await _dataRepository.GetRecords(folder);
                    _response.Result = list;
                }
                else
                {
                    _logger.LogInformation("GetDataConfiguration: Get file content");
                    string content = await _dataRepository.GetRecord(folder, name);
                    _response.Result = content;
                }
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages
                     = new List<string>() { ex.ToString() };
                _logger.LogError($"GetDataConfiguration: Error getting data configuration from folder: {ex}");
            }
            _logger.LogInformation("GetDataConfiguration: Complete");
            return _response;
        }

        [Function("DataConfiguration")]
        public async Task<ResponseDto> SaveConfiguration([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            _logger.LogInformation("SaveConfiguration: Starting");

            try
            {
                string storageAccount = req.GetStorageKey();
                string folder = req.GetQuery("folder", true);
                string name = req.GetQuery("Name", true);
                var stringBody = await new StreamReader(req.Body).ReadToEndAsync();
                _dataRepository.SetConnectionString(storageAccount);
                await _dataRepository.SaveRecord(folder, name, stringBody);
                _response.Result = stringBody;
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages
                     = new List<string>() { ex.ToString() };
                _logger.LogError($"SaveConfiguration: Error getting data configuration from folder: {ex}");
            }
            _logger.LogInformation("SaveConfiguration: Complete");
            return _response;
        }
    }
}
