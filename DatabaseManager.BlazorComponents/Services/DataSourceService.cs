using AutoMapper;
using DatabaseManager.BlazorComponents.Extensions;
using DatabaseManager.BlazorComponents.Models;
using DatabaseManager.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DatabaseManager.BlazorComponents.Services
{
    public class DataSourceService : BaseService, IDataSourceService
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly SingletonServices _settings;
        private readonly IMapper _mapper;

        public DataSourceService(IHttpClientFactory clientFactory, SingletonServices settings, IMapper mapper) : base(clientFactory)
        {
            _clientFactory = clientFactory;
            _settings = settings;
            _mapper = mapper;
        }

        public async Task<T> CreateDataSourceAsync<T>(ConnectParameters connector)
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

        public async Task<T> DeleteDataSourceAsync<T>(string name)
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

        public async Task<T> GetAllDataSourcesAsync<T>()
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

        public async Task<T> GetDataSourceByNameAsync<T>(string name)
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

        public async Task<T> UpdateDataSourceAsync<T>(ConnectParameters connector)
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
    }
}
