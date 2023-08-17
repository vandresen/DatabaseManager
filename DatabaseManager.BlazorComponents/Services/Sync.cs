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
    public class Sync : BaseService, ISync
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly SingletonServices _settings;
        private string url;

        public Sync(IHttpClientFactory clientFactory, SingletonServices settings) : base(clientFactory)
        {
            _clientFactory = clientFactory;
            _settings = settings;
        }

        public async Task<T> GetDataObjects<T>(string sourceName)
        {
            ResponseDto responseDto = new ResponseDto();
            if (string.IsNullOrEmpty(SD.DataConfigurationAPIBase)) url = $"api/Sync/{sourceName}";
            else url = SD.DataConfigurationAPIBase.BuildFunctionUrl($"/api/GetIndexDataObjects/{sourceName}", $"", SD.DataConfigurationKey);
            Console.WriteLine(url);
            return await this.SendAsync<T>(new ApiRequest()
            {
                ApiType = SD.ApiType.GET,
                AzureStorage = _settings.AzureStorage,
                Url = url
            });
        }

        public async Task<T> TransferIndexObjects<T>(object body)
        {
            ResponseDto responseDto = new ResponseDto();
            if (string.IsNullOrEmpty(SD.DataConfigurationAPIBase)) url = $"api/Sync";
            else url = SD.DataConfigurationAPIBase.BuildFunctionUrl("/api/CopyIndexObject", $"", SD.DataConfigurationKey);
            Console.WriteLine(url);
            return await this.SendAsync<T>(new ApiRequest()
            {
                ApiType = SD.ApiType.POST,
                AzureStorage = _settings.AzureStorage,
                Url = url,
                Data = body
            });
        }
    }
}
