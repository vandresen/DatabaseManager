using System.IO;
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
        private readonly IConfigurationFileService _configurationFile;
        private readonly IFileStorageService _fileStorage;
        private readonly string dataAccessDefFolder = "connectdefinition";
        private IDataTransfer _databaseTransfer;
        private IDataTransfer _csvTransfer;
        private IDataTransfer _lasTransfer;
        private TransferParameters _transParm;
        private ConnectParametersDto _sourceParm;
        private ConnectParametersDto _targetParm;
        private string _referenceJson;

        public DataTransfer(ILoggerFactory loggerFactory, IDataSourceService ds,
            IConfigurationFileService configurationFile, IConfiguration configuration,
            IFileStorageService fileStorage)
        {
            _logger = loggerFactory.CreateLogger<DataTransfer>();
            _response = new ResponseDto();
            _fileStorage = fileStorage;
            _configuration = configuration;
            _configurationFile = configurationFile;
            _ds = ds;
            _databaseTransfer = new DatabaseTransfer();
            _csvTransfer = new CSVTransfer(_fileStorage);
            _lasTransfer = new LASTransfer(_fileStorage);
            SD.DataSourceAPIBase = _configuration.GetValue<string>("DataSourceAPI");
            SD.DataSourceKey = _configuration["DataSourceKey"];
            SD.DataConfigurationAPIBase = _configuration["DataConfigurationAPI"];
            SD.DataConfigurationKey = _configuration["DataConfigurationKey"];
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
                ResponseDto dsResponse = await _ds.GetDataSourceByNameAsync<ResponseDto>(name);
                if (dsResponse != null && dsResponse.IsSuccess)
                {
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

        [Function("CopyDatabaseObject")]
        public async Task<ResponseDto> CopyDatabase([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            _logger.LogInformation("Data transfer copy database object: Starting.");
            try
            {
                SD.AzureStorageKey = req.GetStorageKey();
                _fileStorage.SetConnectionString(SD.AzureStorageKey); 
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                TransferParameters transParm = JsonConvert.DeserializeObject<TransferParameters>(requestBody);
                if (transParm == null)
                {
                    _logger.LogError("TransferData: error missing transfer parameters");
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { "Data transfer copy database object: Missing transfer parameters" };
                    return _response;
                }
                if(string.IsNullOrEmpty(transParm.Table))
                {
                    _logger.LogError("TransferData: transfer parameter is missing table name");
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { "Data transfer copy database object: Missing table name in tansfer parameters" };
                    return _response;
                }
                if (transParm.SourceType == "DataBase")
                {
                    ResponseDto sourceResponse = await _ds.GetDataSourceByNameAsync<ResponseDto>(transParm.SourceName);
                    ResponseDto targetResponse = await _ds.GetDataSourceByNameAsync<ResponseDto>(transParm.TargetName);
                    ResponseDto conFileResponse = await _configurationFile.GetConfigurationFileAsync<ResponseDto>("ReferenceTables.json");
                    ResponseDto accessDefResponse = await _configurationFile.GetConfigurationFileAsync<ResponseDto>("PPDMDataAccess.json");
                    bool sourceAccepted = sourceResponse != null && sourceResponse.IsSuccess;
                    bool targetAccepted = sourceResponse != null && sourceResponse.IsSuccess;
                    bool conFileAccepted = conFileResponse != null && conFileResponse.IsSuccess;
                    bool accessDefAccepted = accessDefResponse != null && accessDefResponse.IsSuccess;
                    if (sourceAccepted && targetAccepted && conFileAccepted && accessDefAccepted) 
                    {
                        ConnectParametersDto sourceParm = JsonConvert.DeserializeObject<ConnectParametersDto>(Convert.ToString(sourceResponse.Result));
                        ConnectParametersDto targetParm = JsonConvert.DeserializeObject<ConnectParametersDto>(Convert.ToString(targetResponse.Result));
                        targetParm.DataAccessDefinition = Convert.ToString(accessDefResponse.Result);
                        string referenceJson = Convert.ToString(conFileResponse.Result);
                        await _databaseTransfer.CopyData(transParm, sourceParm, targetParm, referenceJson);
                    }
                    else
                    {
                        _response.IsSuccess = false;
                        _response.ErrorMessages = sourceResponse.ErrorMessages;
                        _response.ErrorMessages = targetResponse.ErrorMessages;
                        _logger.LogError($"Data transfer copy Object: could not get data source");
                    }
                }
                else
                {
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { $"This AP does not support source type {transParm.SourceType}" };
                    _logger.LogError($"Data transfer Delete Object: This AP does not support source type {transParm.SourceType}");
                }
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages
                     = new List<string>() { ex.ToString() };
                _logger.LogError($"Data transfer copy database object: Error copying data object: {ex}");
            }
            return _response;
        }

        [Function("CopyLASObject")]
        public async Task<ResponseDto> CopyLAS([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            _logger.LogInformation("Data transfer copy LAS object: Starting.");
            try
            {
                SD.AzureStorageKey = req.GetStorageKey();
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                await prepare(requestBody);
                if (_transParm.SourceType == "File" && _sourceParm.DataType == "Logs")
                {
                    _sourceParm.DataAccessDefinition = await _fileStorage.ReadFile(dataAccessDefFolder, "LASDataAccess.json");
                    await _lasTransfer.CopyData(_transParm, _sourceParm, _targetParm, _referenceJson);
                }
                else
                {
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { $"This API does not support source type {_transParm.SourceType}" };
                    _logger.LogError($"Data transfer Copy Object: This API does not support source type {_transParm.SourceType}");
                }
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages
                     = new List<string>() { ex.ToString() };
                _logger.LogError($"Data transfer copy LAS object: Error copying data object: {ex}");
            }
            return _response;
        }

        [Function("CopyCSVObject")]
        public async Task<ResponseDto> CopyCSV([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            _logger.LogInformation("Data transfer copy LAS object: Starting.");
            try
            {
                SD.AzureStorageKey = req.GetStorageKey();
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                await prepare(requestBody);
                if (_transParm.SourceType == "File" && _sourceParm.DataType != "Logs")
                {
                    _sourceParm.DataAccessDefinition = await _fileStorage.ReadFile(dataAccessDefFolder, "CSVDataAccess.json");
                    await _csvTransfer.CopyData(_transParm, _sourceParm, _targetParm, _referenceJson);
                }
                else
                {
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { $"This API does not support source type {_transParm.SourceType}" };
                    _logger.LogError($"Data transfer Copy Object: This API does not support source type {_transParm.SourceType}");
                }
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages
                     = new List<string>() { ex.ToString() };
                _logger.LogError($"Data transfer copy LAS object: Error copying data object: {ex}");
            }
            return _response;
        }

        private async Task prepare(string requestBody)
        {
            _fileStorage.SetConnectionString(SD.AzureStorageKey);
            _transParm = JsonConvert.DeserializeObject<TransferParameters>(requestBody);
            if (_transParm == null)
            {
                _logger.LogError("TransferData: error missing transfer parameters");
                Exception error = new Exception($"Data transfer copy database object: Missing transfer parameters");
                throw error;
            }
            if (string.IsNullOrEmpty(_transParm.Table))
            {
                _logger.LogError("TransferData: transfer parameter is missing table name");
                Exception error = new Exception($"Data transfer copy database object: Missing table name in tansfer parameters");
                throw error;
            }
            ResponseDto sourceResponse = await _ds.GetDataSourceByNameAsync<ResponseDto>(_transParm.SourceName);
            ResponseDto targetResponse = await _ds.GetDataSourceByNameAsync<ResponseDto>(_transParm.TargetName);
            bool sourceAccepted = sourceResponse != null && sourceResponse.IsSuccess;
            bool targetAccepted = sourceResponse != null && sourceResponse.IsSuccess;
            if (sourceAccepted && targetAccepted)
            {
                _sourceParm = JsonConvert.DeserializeObject<ConnectParametersDto>(Convert.ToString(sourceResponse.Result));
                _targetParm = JsonConvert.DeserializeObject<ConnectParametersDto>(Convert.ToString(targetResponse.Result));
            }
            else
            {
                _response.IsSuccess = false;
                _response.ErrorMessages = sourceResponse.ErrorMessages;
                _response.ErrorMessages = targetResponse.ErrorMessages;
                _logger.LogError($"Data transfer copy Object: could not get data source");
                Exception error = new Exception($"Data transfer copy database object: Errors getting Datasource info");
                throw error;
            }
            _targetParm.DataAccessDefinition = await _fileStorage.ReadFile(dataAccessDefFolder, "PPDMDataAccess.json");
            _referenceJson = await _fileStorage.ReadFile(dataAccessDefFolder, "ReferenceTables.json");
        }
    }
}
