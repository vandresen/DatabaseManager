using DatabaseManager.Services.DataQC.Extensions;
using DatabaseManager.Services.DataQC.Models;

namespace DatabaseManager.Services.DataQC.Services
{
    public class DataSourceService : BaseService, IDataSourceService
    {
        private readonly IHttpClientFactory _clientFactory;

        public DataSourceService(IHttpClientFactory clientFactory) : base(clientFactory)
        {
            _clientFactory = clientFactory;
        }

        public async Task<T> GetDataSourceByNameAsync<T>(string name)
        {
            string url = SD.DataSourceAPIBase.BuildFunctionUrl($"/api/GetDataSource/{name}", "", SD.DataSourceKey);
            return await this.SendAsync<T>(new ApiRequest()
            {
                ApiType = SD.ApiType.GET,
                Url = url
            });
        }
    }
}
