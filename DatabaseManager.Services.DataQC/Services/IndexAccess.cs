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

        public async Task<T> GetEntiretyIndexes<T>(string dataSource, string dataType, string entiretyName, string parentType)
        {
            string url = SD.IndexAPIBase.BuildFunctionUrl($"/EntiretyIndexes", 
                $"Name={dataSource}&DataType={dataType}&EntiretyName={entiretyName}&ParentType={parentType}", SD.IndexKey);
            return await this.SendAsync<T>(new ApiRequest()
            {
                ApiType = SD.ApiType.GET,
                AzureStorage = SD.AzureStorageKey,
                Url = url
            });
        }

        public async Task<T> GetIndexes<T>(string dataSource, string project, string dataType)
        {
            string url = SD.IndexAPIBase.BuildFunctionUrl($"/QueryIndex", $"Name={dataSource}&DataType={dataType}&Project={project}", SD.IndexKey);
            return await this.SendAsync<T>(new ApiRequest()
            {
                ApiType = SD.ApiType.GET,
                AzureStorage = SD.AzureStorageKey,
                Url = url
            });
        }
    }
}
