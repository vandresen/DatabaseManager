using DatabaseManager.Services.DataQuality.Extensions;
using DatabaseManager.Services.DataQuality.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DatabaseManager.Services.DataQuality.Services
{
    public class DataSourceService : BaseService, IDataSourceService
    {
        private readonly ILogger<DataSourceService> _logger;
        private readonly string _dataSourceAPIBase;
        private readonly string _dataSourceApiKey;

        public DataSourceService(IHttpClientFactory clientFactory, ILogger<DataSourceService> logger, IConfiguration configuration) : base(clientFactory)
        {
            _logger = logger;

            _dataSourceAPIBase = configuration["DataSourceAPI"]
                ?? throw new InvalidOperationException("DataSourceAPI is not configured");

            _dataSourceApiKey = configuration["DataSourceKey"]
                ?? throw new InvalidOperationException("DataSourceKey is not configured");
        }

        public async Task<T> GetDataSourceByNameAsync<T>(string name)
        {
            string url = _dataSourceAPIBase.BuildFunctionUrl($"/api/GetDataSource/{name}", "", _dataSourceApiKey);
            _logger.LogInformation($"Retrieving data sources data from url {url}");
            return await this.SendAsync<T>(new ApiRequest()
            {
                ApiType = SD.ApiType.GET,
                Url = url
            });
        }
    }
}
