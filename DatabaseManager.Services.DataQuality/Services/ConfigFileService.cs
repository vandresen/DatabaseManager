using DatabaseManager.Services.DataQuality.Extensions;
using DatabaseManager.Services.DataQuality.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DatabaseManager.Services.DataQuality.Services
{
    public class ConfigFileService : BaseService, IConfigFileService
    {
        private readonly ILogger<ConfigFileService> _logger;
        private readonly string _configApiBase;
        private readonly string _configApiKey;
        private readonly string folder = "connectdefinition";
        private readonly DataQcExecutionContext _execContext;

        public ConfigFileService(IHttpClientFactory clientFactory, ILogger<ConfigFileService> logger,
            IConfiguration configuration, DataQcExecutionContext execContext) : base(clientFactory)
        {
            _logger = logger;
            _execContext = execContext;
            _configApiBase = configuration["DataConfigurationAPI"]
                ?? throw new InvalidOperationException("DataConfigurationAPI is not configured");

            _configApiKey = configuration["DataConfigurationKey"]
                ?? throw new InvalidOperationException("DataConfigurationKey is not configured");
        }

        public async Task<T> GetConfigurationFileAsync<T>(string name)
        {
            string url = _configApiBase.BuildFunctionUrl($"/api/GetDataConfiguration/", $"folder={folder}&Name={name}", _configApiKey);

            _logger.LogInformation("Retrieving configuration file {ConfigName}", name);

            return await SendAsync<T>(new ApiRequest()
            {
                ApiType = SD.ApiType.GET,
                AzureStorage = _execContext.AzureStorageConnection,
                Url = url
            });
        }
    }
}
