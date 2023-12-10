using DatabaseManager.Services.DataQC.Extensions;
using DatabaseManager.Services.DataQC.Models;

namespace DatabaseManager.Services.DataQC.Services
{
    public class IndexAccess : BaseService, IIndexAccess
    {
        private readonly IHttpClientFactory _clientFactory;

        public IndexAccess(IHttpClientFactory clientFactory) : base(clientFactory)
        {
            _clientFactory = clientFactory;
        }
        public async Task<T> GetIndexes<T>(string dataSource, string dataType)
        {
            string url = SD.IndexAPIBase.BuildFunctionUrl($"/api/QueryIndex", $"Name={dataSource}&DataType={dataType}", SD.IndexKey);
            return await this.SendAsync<T>(new ApiRequest()
            {
                ApiType = SD.ApiType.GET,
                AzureStorage = SD.AzureStorageKey,
                Url = url
            });
        }
    }
}
