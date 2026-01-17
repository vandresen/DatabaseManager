using DatabaseManager.Services.DataQuality.Extensions;
using DatabaseManager.Services.DataQuality.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DatabaseManager.Services.DataQuality.Services
{
    public class IndexAccess : BaseService, IIndexAccess
    {
        private readonly ILogger<IndexAccess> _logger;
        private readonly string _indexAPIBase;
        private readonly string _indexApiKey;
        private readonly DataQcExecutionContext _execContext;

        public IndexAccess(IHttpClientFactory clientFactory, ILogger<IndexAccess> logger, IConfiguration configuration,
            DataQcExecutionContext execContext) : base(clientFactory)
        {
            _logger = logger;
            _execContext = execContext; 

            _indexAPIBase = configuration["IndexAPI"]
                ?? throw new InvalidOperationException("IndexAPI is not configured");

            _indexApiKey = configuration["IndexKey"]
                ?? throw new InvalidOperationException("IndexKey is not configured");
        }

        public async Task<T> GetEntiretyIndexes<T>(string dataSource, string dataType, string entiretyName, string parentType)
        {
            string url = _indexAPIBase.BuildFunctionUrl($"/EntiretyIndexes",
                $"Name={dataSource}&DataType={dataType}&EntiretyName={entiretyName}&ParentType={parentType}", _indexApiKey);
            _logger.LogInformation($"Retrieving entirety data from url {url}");
            return await this.SendAsync<T>(new ApiRequest()
            {
                ApiType = SD.ApiType.GET,
                AzureStorage = _execContext.AzureStorageConnection,
                Url = url
            });
        }

        public async Task<T> GetIndexes<T>(string dataSource, string project, string dataType)
        {
            string url = _indexAPIBase.BuildFunctionUrl($"/QueryIndex", $"Name={dataSource}&DataType={dataType}&Project={project}", _indexApiKey);
            _logger.LogInformation($"Retrieving index data from url {url}");
            return await this.SendAsync<T>(new ApiRequest()
            {
                ApiType = SD.ApiType.GET,
                AzureStorage = _execContext.AzureStorageConnection,
                Url = url
            });
        
        }
    }
}
