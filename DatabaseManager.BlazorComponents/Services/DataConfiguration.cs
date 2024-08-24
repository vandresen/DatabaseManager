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
    public class DataConfiguration : BaseService, IDataConfiguration
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly SingletonServices _settings;
        private readonly string folder = "connectdefinition";
        private string url;

        public DataConfiguration(IHttpClientFactory clientFactory, SingletonServices settings) : base(clientFactory)
        {
            _clientFactory = clientFactory;
            _settings = settings;
        }

        public async Task<T> DeleteRecord<T>(string name)
        {
            ResponseDto responseDto = new ResponseDto();
            if (string.IsNullOrEmpty(_settings.DataConfigurationAPIBase)) url = $"api/DataConfiguration?folder={folder}&name={name}";
            else url = _settings.DataConfigurationAPIBase.BuildFunctionUrl("/api/GetDataConfiguration", $"folder={folder}&name={name}", _settings.DataConfigurationKey);
            Console.WriteLine(url);
            return await this.SendAsync<T>(new ApiRequest()
            {
                ApiType = SD.ApiType.DELETE,
                AzureStorage = _settings.AzureStorage,
                Url = url
            });
        }

        public async Task<T> GetRecord<T>(string name)
        {
            ResponseDto responseDto = new ResponseDto();
            if (string.IsNullOrEmpty(_settings.DataConfigurationAPIBase)) url = $"api/DataConfiguration?folder={folder}&name={name}";
            else url = _settings.DataConfigurationAPIBase.BuildFunctionUrl("/api/GetDataConfiguration", $"folder={folder}&name={name}", _settings.DataConfigurationKey);
            Console.WriteLine($"GetRecord URL:{url}");
            return await this.SendAsync<T>(new ApiRequest()
            {
                ApiType = SD.ApiType.GET,
                AzureStorage = _settings.AzureStorage,
                Url = url
            });
        }

        public async Task<T> GetRecords<T>()
        {
            ResponseDto responseDto = new ResponseDto();
            if (string.IsNullOrEmpty(_settings.DataConfigurationAPIBase)) url = $"api/DataConfiguration?folder={folder}";
            else url = _settings.DataConfigurationAPIBase.BuildFunctionUrl("/api/GetDataConfiguration", $"folder={folder}", _settings.DataConfigurationKey);
            Console.WriteLine($"GetRecords URL:{url}");
            return await this.SendAsync<T>(new ApiRequest()
            {
                ApiType = SD.ApiType.GET,
                AzureStorage = _settings.AzureStorage,
                Url = url
            });
        }

        public async Task<T> SaveRecords<T>(string name, object body)
        {
            ResponseDto responseDto = new ResponseDto();
            if (string.IsNullOrEmpty(_settings.DataConfigurationAPIBase)) url = $"api/DataConfiguration?folder={folder}&name={name}";
            else url = _settings.DataConfigurationAPIBase.BuildFunctionUrl("/api/DataConfiguration", $"folder={folder}&name={name}", _settings.DataConfigurationKey);
            Console.WriteLine(url);
            return await this.SendAsync<T>(new ApiRequest()
            {
                ApiType = SD.ApiType.POST,
                AzureStorage = _settings.AzureStorage,
                Url = url,
                Data= body
            });
        }
    }
}
