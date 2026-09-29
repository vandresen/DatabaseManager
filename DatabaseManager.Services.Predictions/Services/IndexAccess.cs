using DatabaseManager.Services.Predictions.Extensions;
using DatabaseManager.Services.Predictions.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Web;

namespace DatabaseManager.Services.Predictions.Services
{
    public class IndexAccess : BaseService, IIndexAccess
    {
        private readonly ILogger<IndexAccess> _logger;

        public IndexAccess(IHttpClientFactory clientFactory, ILogger<IndexAccess> logger) : base(clientFactory)
        {
            _logger = logger;
        }

        public async Task<T> GetDescendants<T>(int id, string dataSource, string project, string storageConnection)
        {
            string url = SD.IndexAPIBase.BuildFunctionUrl($"/GetDescendants/{id}", $"Name={dataSource}&Project={project}", SD.IndexKey);
            return await this.SendAsync<T>(new ApiRequest()
            {
                ApiType = SD.ApiType.GET,
                AzureStorage = storageConnection,
                Url = url
            });
        }

        public async Task<T> GetIndex<T>(int id, string project, string storageConnection)
        {
            string url = SD.IndexAPIBase.BuildFunctionUrl($"/Index/{id}", $"Project={project}", SD.IndexKey);
            _logger.LogInformation($"Retrieving root index data from url {url}");
            return await this.SendAsync<T>(new ApiRequest()
            {
                ApiType = SD.ApiType.GET,
                AzureStorage = storageConnection,
                Url = url
            });
        }

        public async Task<List<IndexDto>> GetIndexes(
            string dataSource,
            string project,
            string dataType,
            string dataName,
            string dataKey,
            string storageConnection)
        {
            string url;
            var qp = HttpUtility.ParseQueryString(string.Empty);
            qp["Name"] = dataSource;
            qp["DataType"] = dataType;
            qp["Project"] = project;
            qp["DataName"] = dataName;
            qp["DataKey"] = dataKey;
            string query = qp.ToString();

            if (SD.Sqlite)
            {
                url = SD.IndexAPIBase.BuildFunctionUrl(
                    "/api/indexes/search",
                    query,
                    SD.IndexKey);

                return await SendAsync<List<IndexDto>>(new ApiRequest
                {
                    ApiType = SD.ApiType.GET,
                    AzureStorage = storageConnection,
                    Url = url
                });
            }

            url = SD.IndexAPIBase.BuildFunctionUrl(
                "/QueryIndex",
                query,
                SD.IndexKey);

            ResponseDto response = await SendAsync<ResponseDto>(new ApiRequest
            {
                ApiType = SD.ApiType.GET,
                AzureStorage = storageConnection,
                Url = url
            });

            if (response == null || !response.IsSuccess || response.Result == null)
                return new List<IndexDto>();

            if (response.Result is JsonElement element)
                return element.Deserialize<List<IndexDto>>() ?? new List<IndexDto>();

            return response.Result as List<IndexDto> ?? new List<IndexDto>();
        }

        public async Task<ResponseDto> GetNeighbors(int id, string dataSource, string failRule, string path, string project)
        {
            var queryParams = HttpUtility.ParseQueryString(string.Empty);
            queryParams["Name"] = dataSource;
            queryParams["Project"] = project;
            queryParams["failRule"] = failRule;
            queryParams["depthAttribute"] = path;
            string queryString = queryParams.ToString();

            string url = "";
            if (SD.Sqlite)
            {
                url = SD.IndexAPIBase.BuildFunctionUrl($"/GetNeighbors/{id}", queryString, SD.IndexKey);
                return await this.SendAsync<ResponseDto>(new ApiRequest()
                {
                    ApiType = SD.ApiType.GET,
                    Url = url
                });
            }         

            url = SD.IndexAPIBase.BuildFunctionUrl($"/GetNeighbors/{id}", queryString,SD.IndexKey);
            var result = await this.SendAsync<ResponseDto>(new ApiRequest()
            {
                ApiType = SD.ApiType.GET,
                Url = url
            });
            return result;
        }

        public async Task<T> GetRootIndex<T>(string dataSource, string project, string storageConnection)
        {
            string url = "";
            if (SD.Sqlite)
            {
                url = SD.IndexAPIBase.BuildFunctionUrl($"/Index/1", $"project={project}", SD.IndexKey);
            }
            else
            {
                url = SD.IndexAPIBase.BuildFunctionUrl("/DmIndexes", $"Name={dataSource}&Node=/&Level=0", SD.IndexKey);
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
            if (SD.Sqlite)
            {
                url = SD.IndexAPIBase.BuildFunctionUrl($"/Index", $"project={project}", SD.IndexKey);
            }
            else
            {
                url = SD.IndexAPIBase.BuildFunctionUrl("/Indexes", $"Name={dataSource}&Datatype={index.DataType}&Parentid={index.ParentId}", SD.IndexKey);
            }
            _logger.LogInformation($"Url = {url}");
            return await this.SendAsync<T>(new ApiRequest()
            {
                ApiType = SD.ApiType.POST,
                AzureStorage = storageConnection,
                Url = url,
                Data = index
            });
        }

        public Task<T> InsertIndexes<T>(List<IndexDto> indexes, string dataSource, string project, string storageConnection)
        {
            throw new NotImplementedException();
        }

        public async Task<T> UpdateIndexes<T>(List<IndexDto> indexes, string dataSource, string project, string storageConnection)
        {
            string url;
            if (SD.Sqlite)
            {
                url = SD.IndexAPIBase.BuildFunctionUrl($"/Indexes", $"Name={dataSource}&Project={project}", SD.IndexKey);
            }
            else
            {
                url = SD.IndexAPIBase.BuildFunctionUrl($"/Indexes", $"Name={dataSource}", SD.IndexKey);
            }
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
