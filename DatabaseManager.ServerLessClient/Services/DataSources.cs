using DatabaseManager.ServerLessClient.Helpers;
using DatabaseManager.ServerLessClient.Models;

namespace DatabaseManager.ServerLessClient.Services
{
    public class DataSources: BaseService, IDataSources
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly BlazorSingletonService _settings;

        public DataSources(IHttpClientFactory clientFactory, BlazorSingletonService settings) : base(clientFactory)
        {
            _clientFactory = clientFactory;
            _settings = settings;
        }
        public async Task<T> CreateSource<T>(ConnectParameters connector)
        {
            string url = _settings.DataSourceAPI.BuildFunctionUrl($"/api/SaveDataSource", "", _settings.DataSourceKey);
            Console.WriteLine($"CreateDataSourceAsync: url = {url}");
            return await this.SendAsync<T>(new ApiRequest()
            {
                ApiType = SD.ApiType.POST,
                AzureStorage = _settings.AzureStorage,
                Url = url,
                Data = connector
            });
        }

        public async Task<T> DeleteSource<T>(string name)
        {
            string url = _settings.DataSourceAPI.BuildFunctionUrl($"/api/DeleteDataSource/{name}", "", _settings.DataSourceKey);
            Console.WriteLine($"DeleteDataSourceAsync: url = {url}");
            return await this.SendAsync<T>(new ApiRequest()
            {
                ApiType = SD.ApiType.DELETE,
                AzureStorage = _settings.AzureStorage,
                Url = url
            });
        }

        public async Task<T> GetSource<T>(string name)
        {
            string url = _settings.DataSourceAPI.BuildFunctionUrl($"/api/GetDataSource/{name}", "", _settings.DataSourceKey);
            Console.WriteLine($"GetAllDataSources: url = {url}");
            return await this.SendAsync<T>(new ApiRequest()
            {
                ApiType = SD.ApiType.GET,
                AzureStorage = _settings.AzureStorage,
                Url = url
            });
        }

        public async Task<T> GetSources<T>()
        {
            string url = _settings.DataSourceAPI.BuildFunctionUrl($"/api/GetDataSources", "", _settings.DataSourceKey);
            Console.WriteLine($"GetAllDataSources: url = {url}");
            return await this.SendAsync<T>(new ApiRequest()
            {
                ApiType = SD.ApiType.GET,
                AzureStorage = _settings.AzureStorage,
                Url = url
            });
        }

        public async Task<T> UpdateSource<T>(ConnectParameters connector)
        {
            string url = _settings.DataSourceAPI.BuildFunctionUrl($"/api/SaveDataSource", "", _settings.DataSourceKey);
            Console.WriteLine($"CreateDataSourceAsync: url = {url}");
            return await this.SendAsync<T>(new ApiRequest()
            {
                ApiType = SD.ApiType.POST,
                AzureStorage = _settings.AzureStorage,
                Url = url,
                Data = connector
            });
        }
    }
}
