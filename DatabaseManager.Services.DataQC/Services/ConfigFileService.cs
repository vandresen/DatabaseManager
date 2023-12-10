using DatabaseManager.Services.DataQC.Extensions;
using DatabaseManager.Services.DataQC.Models;

namespace DatabaseManager.Services.DataQC.Services
{
    public class ConfigFileService : BaseService, IConfigFileService
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly string folder = "connectdefinition";

        public ConfigFileService(IHttpClientFactory clientFactory) : base(clientFactory)
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
