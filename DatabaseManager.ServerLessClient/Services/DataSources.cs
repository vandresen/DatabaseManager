using DatabaseManager.ServerLessClient.Models;
using DatabaseManager.BlazorComponents.Extensions;
using System.Xml.Linq;

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
            string url = SD.DataSourceAPIBase.BuildFunctionUrl($"/api/SaveDataSource", "", SD.DataSourceKey);
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
            string url = SD.DataSourceAPIBase.BuildFunctionUrl($"/api/DeleteDataSource/{name}", "", SD.DataSourceKey);
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
            string url = SD.DataSourceAPIBase.BuildFunctionUrl($"/api/GetDataSource/{name}", "", SD.DataSourceKey);
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
            string url = SD.DataSourceAPIBase.BuildFunctionUrl($"/api/GetDataSources", "", SD.DataSourceKey);
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
            string url = SD.DataSourceAPIBase.BuildFunctionUrl($"/api/SaveDataSource", "", SD.DataSourceKey);
            Console.WriteLine($"CreateDataSourceAsync: url = {url}");
            return await this.SendAsync<T>(new ApiRequest()
            {
                ApiType = SD.ApiType.POST,
                AzureStorage = _settings.AzureStorage,
                Url = url,
                Data = connector
            });
            //ResponseDto response = await _ds.CreateDataSourceAsync<ResponseDto>(connectParameters);
            //if (!response.IsSuccess)
            //{
            //    throw new ApplicationException(String.Join("; ", response.ErrorMessages));
            //}
        }
    }
}
