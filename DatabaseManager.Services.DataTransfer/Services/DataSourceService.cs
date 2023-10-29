using DatabaseManager.Services.DataTransfer.Extensions;
using DatabaseManager.Services.DataTransfer.Models;

namespace DatabaseManager.Services.DataTransfer.Services
{
    public class DataSourceService : BaseService, IDataSourceService
    {
        private readonly IHttpClientFactory _clientFactory;

        public DataSourceService(IHttpClientFactory clientFactory) : base(clientFactory)
        {
            _clientFactory = clientFactory;
        }

        public async Task<T> GetDataSourceByNameAsync<T>(string name, string storageAccount)
        {
            string url = SD.DataSourceAPIBase.BuildFunctionUrl($"/api/GetDataSource/{name}", "", SD.DataSourceKey);
            return await SendAsync<T>(new ApiRequest()
            {
                ApiType = SD.ApiType.GET,
                AzureStorage = storageAccount,
                Url = url
            });
        }
    }
}
