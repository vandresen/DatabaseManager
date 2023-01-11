using DatabaseManager.BlazorComponents.Models;
using DatabaseManager.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.BlazorComponents.Services
{
    public class DataSourceService : BaseService, IDataSourceService
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly SingletonServices _settings;

        public DataSourceService(IHttpClientFactory clientFactory, SingletonServices settings) : base(clientFactory)
        {
            _clientFactory = clientFactory;
            _settings = settings;
        }

        public Task<T> CreateDataSourceAsync<T>(ConnectParametersDto connector)
        {
            throw new System.NotImplementedException();
        }

        public Task<T> DeleteDataSourceAsync<T>(string name)
        {
            throw new System.NotImplementedException();
        }

        public async Task<T> GetAllDataSourcesAsync<T>()
        {
            string key = "";
            if (!string.IsNullOrEmpty(SD.DataSourceKey)) key = "?code=" + SD.DataSourceKey;
            string url = SD.DataSourceAPIBase + "/api/GetDataSources" + key;
            Console.WriteLine($"GetAllDataSources: url = {url}");
            return await this.SendAsync<T>(new ApiRequest()
            {
                ApiType = SD.ApiType.GET,
                AzureStorage = _settings.AzureStorage,
                Url = url
            });
        }

        public Task<T> GetDataSourceByNameAsync<T>(string name)
        {
            throw new System.NotImplementedException();
        }

        public Task<T> UpdateDataSourceAsync<T>(ConnectParametersDto connector)
        {
            throw new System.NotImplementedException();
        }
    }
}
