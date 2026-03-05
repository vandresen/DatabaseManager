using DatabaseManager.Services.Predictions.Extensions;
using DatabaseManager.Services.Predictions.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Configuration;
using System.Data.Common;

namespace DatabaseManager.Services.Predictions.Services
{
    public class IndexAccess : BaseService, IIndexAccess
    {
        private readonly ILogger<IndexAccess> _logger;
        private readonly string _indexAPIBase;
        private readonly string _indexApiKey;

        public IndexAccess(IHttpClientFactory clientFactory, ILogger<IndexAccess> logger, IConfiguration configuration) : base(clientFactory)
        {
            _logger = logger;

            _indexAPIBase = configuration["IndexAPI"]
                ?? throw new InvalidOperationException("IndexAPI is not configured");

            _indexApiKey = configuration["IndexKey"]
                ?? throw new InvalidOperationException("IndexKey is not configured");
        }

        public async Task<T> GetDescendants<T>(int id, string dataSource, string project, string storageConnection)
        {
            string url = _indexAPIBase.BuildFunctionUrl($"/GetDescendants/{id}", $"Name={dataSource}&Project={project}", _indexApiKey);
            _logger.LogInformation($"Retrieving index data from url {url}");
            return await this.SendAsync<T>(new ApiRequest()
            {
                ApiType = SD.ApiType.GET,
                AzureStorage = storageConnection,
                Url = url
            });
        }

        public async Task<T> GetIndexes<T>(string dataSource, string project, string dataType, string storageConnection)
        {
            string url = _indexAPIBase.BuildFunctionUrl($"/QueryIndex", $"Name={dataSource}&DataType={dataType}&Project={project}", _indexApiKey);
            _logger.LogInformation($"Retrieving index data from url {url}");
            return await this.SendAsync<T>(new ApiRequest()
            {
                ApiType = SD.ApiType.GET,
                AzureStorage = storageConnection,
                Url = url
            });

        }

        public async Task<T> UpdateIndexes<T>(List<IndexDto> indexes, string dataSource, string project, string storageConnection)
        {
            string url = _indexAPIBase.BuildFunctionUrl($"/Indexes", $"Name={dataSource}&Project={project}", _indexApiKey);
            return await SendAsync<T>(new ApiRequest()
            {
                ApiType = SD.ApiType.PUT,
                AzureStorage = storageConnection,
                Url = url,
                Data = indexes
            });
        }
    }
}
