using DatabaseManager.Services.Predictions.Extensions;
using DatabaseManager.Services.Predictions.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Data.Common;

namespace DatabaseManager.Services.Predictions.Services
{
    public class IndexAccess : BaseService, IIndexAccess
    {
        private readonly ILogger<IndexAccess> _logger;
        private readonly string _indexAPIBase;
        private readonly string _indexApiKey;
        private readonly bool _sqlLite;

        public IndexAccess(IHttpClientFactory clientFactory, ILogger<IndexAccess> logger, IConfiguration configuration) : base(clientFactory)
        {
            _logger = logger;

            _indexAPIBase = configuration["IndexAPI"]
                ?? throw new InvalidOperationException("IndexAPI is not configured");

            _indexApiKey = configuration["IndexKey"]
                ?? throw new InvalidOperationException("IndexKey is not configured");

            if (!bool.TryParse(configuration["Sqlite"], out _sqlLite))
            {
                throw new InvalidOperationException("Sqlite is not configured or is not a valid boolean");
            }
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

        public async Task<T> GetIndex<T>(int id, string project, string storageConnection)
        {
            string url = _indexAPIBase.BuildFunctionUrl($"/Index/{id}", $"Project={project}", _indexApiKey);
            _logger.LogInformation($"Retrieving root index data from url {url}");
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

        public async Task<T> GetRootIndex<T>(string dataSource, string project, string storageConnection)
        {
            string url = "";
            if (_sqlLite)
            {
                url = _indexAPIBase.BuildFunctionUrl($"/Index/1", $"project={project}", _indexApiKey);
            }
            else
            {
                url = _indexAPIBase.BuildFunctionUrl("/DmIndexes", $"Name={dataSource}&Node=/&Level=0", _indexApiKey);
            }
            _logger.LogInformation($"Url = {url}");
            return await this.SendAsync<T>(new ApiRequest()
            {
                ApiType = SD.ApiType.GET,
                AzureStorage = storageConnection,
                Url = url
            });
        }

        public async Task<T> InsertIndex<T>(IndexDto index, string dataSource, string project, string storageConnection)
        {
            string url = "";
            if (_sqlLite)
            {
                url = _indexAPIBase.BuildFunctionUrl($"/Index", $"project={project}", _indexApiKey);
            }
            else
            {
                throw new NotImplementedException();
                //url = _indexAPIBase.BuildFunctionUrl("/DmIndexes", $"Name={dataSource}&Node=/&Level=0", _indexApiKey);
            }
            _logger.LogInformation($"Url = {url}");
            return await this.SendAsync<T>(new ApiRequest()
            {
                ApiType = SD.ApiType.POST,
                AzureStorage = storageConnection,
                Url = url
            });
        }

        public Task<T> InsertIndexes<T>(List<IndexDto> indexes, string dataSource, string project, string storageConnection)
        {
            throw new NotImplementedException();
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
