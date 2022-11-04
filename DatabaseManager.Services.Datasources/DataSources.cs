using DatabaseManager.Services.Datasources.Extensions;
using DatabaseManager.Services.Datasources.Models.Dto;
using DatabaseManager.Services.Datasources.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace DatabaseManager.Services.Datasources
{
    public class DataSources
    {
        private readonly IDataSourceRepository _dataSourceRepository;
        protected ResponseDto _response;

        public DataSources(IDataSourceRepository dataSourceRepository)
        {
            _dataSourceRepository = dataSourceRepository;
            this._response = new ResponseDto();
        }

        [FunctionName("GetDataSources")]
        public async Task<IActionResult> GetSources(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("GetDataSources: Starting");
            try
            {
                string storageAccount = req.GetStorageKey();
                List<ConnectParametersDto> sources = await _dataSourceRepository.GetDataSources(storageAccount);
                _response.Result = sources;
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages
                     = new List<string>() { ex.ToString() };
                log.LogError("GetDataSources: Error getting data sources: {ex}");
            }
            log.LogInformation("GetDataSources: Complete");
            return new OkObjectResult(_response);
        }

        [FunctionName("GetDataSource")]
        public async Task<IActionResult> GetSource(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "GetDataSource/{name}")] HttpRequest req,
            string name,
            ILogger log)
        {
            log.LogInformation("GetDataSource: Starting");
            try
            {
                string storageAccount = req.GetStorageKey();
                ConnectParametersDto source = await _dataSourceRepository.GetDataSourceByName(name, storageAccount);
                _response.Result = source;
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages
                     = new List<string>() { ex.ToString() };
                log.LogError("GetDataSource: Error getting data sources: {ex}");
            }

            log.LogInformation("GetDataSource: Complete");
            return new OkObjectResult(_response);
        }

        [FunctionName("SaveDataSource")]
        public async Task<IActionResult> SaveSource(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "SaveDataSource")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("SaveDataSource: Starting");
            try
            {
                string storageAccount = req.GetStorageKey();
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                ConnectParametersDto connector = JsonConvert.DeserializeObject<ConnectParametersDto>(requestBody);
                if (connector == null)
                {
                    log.LogError("SaveDataSource: Error missing connector parameters");
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { "Error missing connector parameters" };
                }
                else
                {
                    ConnectParametersDto source = await _dataSourceRepository.CreateUpdateDataSource(connector, storageAccount);
                    _response.Result = source;
                }
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages
                     = new List<string>() { ex.ToString() };
                log.LogError("SaveDataSource: Error getting data sources: {ex}");
            }

            log.LogInformation("SaveDataSource: Complete");
            return new OkObjectResult(_response);
        }

        [FunctionName("DeleteDataSource")]
        public async Task<IActionResult> DeleteSource(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "DeleteDataSource/{name}")] HttpRequest req,
            string name,
            ILogger log)
        {
            log.LogInformation("DeleteDataSource: Starting");
            try
            {
                string storageAccount = req.GetStorageKey();
                bool isSuccess = await _dataSourceRepository.DeleteDataSource(name, storageAccount);
                _response.Result = isSuccess;
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages
                     = new List<string>() { ex.ToString() };
                log.LogError("DeleteDataSource: Error getting data sources: {ex}");
            }

            log.LogInformation("DeleteDataSource: Complete");
            return new OkObjectResult(_response);
        }
    }
}
