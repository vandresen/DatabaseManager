using System.Net;
using Azure;
using DatabaseManager.Services.DataTransfer.Extensions;
using DatabaseManager.Services.DataTransfer.Models;
using DatabaseManager.Services.DataTransfer.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DatabaseManager.Services.DataTransfer
{
    public class DataTransfer
    {
        private readonly ILogger _logger;
        protected ResponseDto _response;
        private readonly IDataSourceService _ds;
        private readonly IConfiguration _configuration;
        private IDataTransfer _databaseTransfer;
        private IDataTransfer _csvTransfer;
        private IDataTransfer _lasTransfer;

        public DataTransfer(ILoggerFactory loggerFactory, IDataSourceService ds,
            IConfiguration configuration)
        {
            _logger = loggerFactory.CreateLogger<DataTransfer>();
            _response = new ResponseDto();
            _configuration = configuration;
            _ds = ds;
            _databaseTransfer = new DatabaseTransfer();
            _csvTransfer = new CSVTransfer();
            _lasTransfer = new LASTransfer();
            SD.DataSourceAPIBase = _configuration.GetValue<string>("DataSourceAPI");
            SD.DataSourceKey = _configuration["DataSourceKey"];
        }

        [Function("GetDataObjects")]
        public async Task<ResponseDto> DataObjects([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
        {
            _logger.LogInformation("Data transfer Get Data Objects: Starting.");
            try
            {
                string name = req.GetQuery("Name", true);
                string storageAccount = req.GetStorageKey();
                ResponseDto dsResponse = await _ds.GetDataSourceByNameAsync<ResponseDto>(name);
                if (dsResponse != null && dsResponse.IsSuccess)
                {
                    List<string> containers = new List<string>();
                    ConnectParametersDto connectParameter = JsonConvert.DeserializeObject<ConnectParametersDto>(Convert.ToString(dsResponse.Result));
                    if (connectParameter.SourceType == "DataBase")
                    {
                        containers = await _databaseTransfer.GetContainers(connectParameter);
                    }
                    else
                    {
                        if (connectParameter.DataType == "Logs")
                        {
                            connectParameter.ConnectionString = storageAccount;
                            containers = await _lasTransfer.GetContainers(connectParameter);
                        }
                        else
                        {
                            containers = await _csvTransfer.GetContainers(connectParameter);
                        }
                    }
                    _response.Result = containers;
                }
                else
                {
                    _response.IsSuccess = false;
                    _response.ErrorMessages = dsResponse.ErrorMessages;
                    _logger.LogError($"Data transfer Get Data Objects: Could not get the data source");
                }  
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages
                     = new List<string>() { ex.ToString() };
                _logger.LogError($"Data transfer Get Data Objects: Error getting data objects: {ex}");
            }
            return _response;
        }

        [Function("DeleteObject")]
        public async Task<ResponseDto> DeleteDataObject([HttpTrigger(AuthorizationLevel.Function, "delete")] HttpRequestData req)
        {
            _logger.LogInformation("Data transfer Delete Object: Starting.");
            try
            {
                string name = req.GetQuery("Name", true);
                string table = req.GetQuery("Table", true);
                //string storageAccount = req.GetStorageKey();
                ResponseDto dsResponse = await _ds.GetDataSourceByNameAsync<ResponseDto>(name);
                if (dsResponse != null && dsResponse.IsSuccess)
                {
                    //    List<string> containers = new List<string>();
                    ConnectParametersDto connectParameter = JsonConvert.DeserializeObject<ConnectParametersDto>(Convert.ToString(dsResponse.Result));
                    if (connectParameter.SourceType == "DataBase")
                    {
                        _databaseTransfer.DeleteData(connectParameter, table);
                    }
                    else
                    {
                        _response.IsSuccess = false;
                        _response.ErrorMessages = new List<string>() { "This data source does not support delete" };
                        _logger.LogError($"Data transfer Delete Object: This data source does not support delete");
                    }
                    //    _response.Result = containers;
                }
                else
                {
                    _response.IsSuccess = false;
                    _response.ErrorMessages = dsResponse.ErrorMessages;
                    _logger.LogError($"Data transfer Delete Object: could not get data source");
                }
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages
                     = new List<string>() { ex.ToString() };
                _logger.LogError($"Data transfer Delete Object: Error deleteing data object: {ex}");
            }
            return _response;
        }
    }
}
