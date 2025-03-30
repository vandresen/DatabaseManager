using Microsoft.Extensions.Configuration;
using DatabaseManager.Services.Reports.Models;
using DatabaseManager.Services.Reports.Extensions;

namespace DatabaseManager.Services.Reports.Services
{
    public class ConfigFileService : BaseService, IConfigFileService
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly string folder = "connectdefinition";
        private readonly IConfiguration _configuration;

        public ConfigFileService(IHttpClientFactory clientFactory, IConfiguration configuration) : base(clientFactory)
        {
            _clientFactory = clientFactory;
            _configuration = configuration;
        }

        public async Task<T> GetConfigurationFileAsync<T>(string name)
        {
            string url = _configuration["DataConfigurationAPI"].BuildFunctionUrl($"/api/GetDataConfiguration/",
                $"folder={folder}&Name={name}", _configuration["DataConfigurationKey"]);
            return await SendAsync<T>(new ApiRequest()
            {
                ApiType = SD.ApiType.GET,
                AzureStorage = SD.AzureStorageKey,
                Url = url
            });
        }
    }
}
