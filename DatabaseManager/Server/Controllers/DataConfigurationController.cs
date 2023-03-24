using DatabaseManager.Common.Helpers;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Http;
using DatabaseManager.Server.Entities;
using DatabaseManager.Common.Services;
using Microsoft.Extensions.Configuration;
using static MudBlazor.CategoryTypes;
using Azure;
using System.Reflection.Metadata;

namespace DatabaseManager.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DataConfigurationController : ControllerBase
    {
        private readonly IFileStorageServiceCommon _fileStorageService;
        private string _connectionString;
        protected ResponseDto _response;

        public DataConfigurationController(IFileStorageServiceCommon fileStorageService, IConfiguration configuration)
        {
            this._response = new ResponseDto();
            this._fileStorageService = fileStorageService;
            _connectionString = configuration.GetConnectionString("AzureStorageConnection");
        }

        [HttpGet]
        public async Task<ActionResult<object>> Get(string folder, string name = "")
        {
            try
            {
                if(folder == null) 
                {
                    _response.IsSuccess = false;
                    _response.ErrorMessages
                     = new List<string>() { "Folder missing"};
                }
                if(_response.IsSuccess) 
                {
                    GetStorageAccount();
                    _fileStorageService.SetConnectionString(_connectionString);
                    if (string.IsNullOrEmpty(name))
                    {
                        List<string> list = await _fileStorageService.ListFiles(folder);
                        _response.Result = list;
                    }
                    else
                    {
                        string content = await _fileStorageService.ReadFile(folder, name);
                        _response.Result = content;
                    }
                    
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

        [HttpPost]
        public async Task<ActionResult<object>> save(string folder, string name, object body)
        {
            try
            {
                if (folder == null || name == null)
                {
                    _response.IsSuccess = false;
                    _response.ErrorMessages
                     = new List<string>() { "Folder or name is missing" };
                }
                if (_response.IsSuccess)
                {
                    GetStorageAccount();
                    string content = Convert.ToString(body);
                    _fileStorageService.SetConnectionString(_connectionString);
                    await _fileStorageService.SaveFile(folder, name, content);
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

        [HttpDelete]
        public async Task<ActionResult<object>> Delete(string folder, string name)
        {
            try
            {
                if (folder == null || name == null)
                {
                    _response.IsSuccess = false;
                    _response.ErrorMessages
                     = new List<string>() { "Folder or name is missing" };
                }
                if (_response.IsSuccess)
                {
                    GetStorageAccount();
                    _fileStorageService.SetConnectionString(_connectionString);
                    await _fileStorageService.DeleteFile(folder, name);
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
                Exception error = new Exception($"Azure storage key string is not set");
                throw error;
            }
        }
    }
}
