using DatabaseManager.Services.DataTransfer.Extensions;
using DatabaseManager.Services.DataTransfer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.DataTransfer.Services
{
    public class ConfigurationFileService : BaseService, IConfigurationFileService
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly string folder = "connectdefinition";

        public ConfigurationFileService(IHttpClientFactory clientFactory) : base(clientFactory)
        {
            _clientFactory = clientFactory;
        }
        public async Task<T> GetConfigurationFileAsync<T>(string name)
        {
            string url = SD.DataConfigurationAPIBase.BuildFunctionUrl($"/api/GetDataConfiguration/", 
                $"folder={folder}&Name={name}", SD.DataConfigurationKey);
            return await SendAsync<T>(new ApiRequest()
            {
                ApiType = SD.ApiType.GET,
                AzureStorage = SD.AzureStorageKey,
                Url = url
            });
        }
    }
}
