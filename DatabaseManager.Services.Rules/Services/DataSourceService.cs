using DatabaseManager.Services.Rules.Extensions;
using DatabaseManager.Services.Rules.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.Rules.Services
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
