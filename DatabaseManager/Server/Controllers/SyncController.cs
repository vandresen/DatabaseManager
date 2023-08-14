using DatabaseManager.Common.Helpers;
using DatabaseManager.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using DatabaseManager.BlazorComponents.Models;
using static MudBlazor.CategoryTypes;
using DatabaseManager.Common.Data;
using DatabaseManager.Common.DBAccess;

namespace DatabaseManager.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SyncController : ControllerBase
    {
        private readonly ILogger<SyncController> _logger;
        
        private string _connectionString;
        protected ResponseDto _response;

        public SyncController(ILogger<SyncController> logger)
        {
            _logger = logger;
            _response = new ResponseDto();
            
        }

        [HttpGet("{source}")]
        public async Task<ActionResult<object>> Get(string source)
        {
            try
            {
                GetStorageAccount();
                ConnectParameters connector = await GetConnector(source);
                IndexToDatabaseTransfer indexTransfer = new IndexToDatabaseTransfer(_logger, _connectionString);
                List<string> dataObjects = await indexTransfer.GetDataObjectList(connector);
                _response.Result = dataObjects;
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages
                     = new List<string>() { ex.ToString() };
            }
            return _response;
        }

        [HttpPost]
        public async Task<ActionResult<object>> ExecuteSync(SyncParameters syncParams)
        {
            try
            {
                if (syncParams == null) return BadRequest();
                GetStorageAccount();
                ConnectParameters sourceConnector = await GetConnector(syncParams.SourceName);
                ConnectParameters targetConnector = await GetConnector(syncParams.TargetName);
                if (targetConnector.SourceType != "DataBase")
                {
                    string error = $"Target database {targetConnector.SourceName} is not a database";
                    _logger.LogError(error);
                    _response.IsSuccess = false;
                    _response.ErrorMessages
                         = new List<string>() { error };
                }
                else
                {
                    IndexToDatabaseTransfer indexTransfer = new IndexToDatabaseTransfer(_logger, _connectionString);
                    await indexTransfer.TransferDataObject(sourceConnector, targetConnector, syncParams.DataObjectType);
                }
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages
                     = new List<string>() { ex.ToString() };
            }
            return _response;
        }

        private void GetStorageAccount()
        {
            string tmpConnString = Request.Headers["AzureStorageConnection"];
            if (!string.IsNullOrEmpty(tmpConnString)) _connectionString = tmpConnString;
            if (string.IsNullOrEmpty(_connectionString))
            {
                _logger.LogError("Azure storage key string is not set");
                Exception error = new Exception($"Azure storage key string is not set");
                throw error;
            }
        }

        private async Task<ConnectParameters> GetConnector(string connectorStr)
        {
            if (String.IsNullOrEmpty(connectorStr))
            {
                Exception error = new Exception($"DataQc: Connection string is not set");
                throw error;
            }
            Sources so = new Sources(_connectionString);
            ConnectParameters connector = await so.GetSourceParameters(connectorStr);
            return connector;
        }
    }
}
