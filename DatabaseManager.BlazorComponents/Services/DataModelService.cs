using DatabaseManager.BlazorComponents.Extensions;
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
    public class DataModelService : BaseService, IDataModelService
    {
        private readonly IHttpClientFactory clientFactory;
        private readonly SingletonServices _settings;

        public DataModelService(IHttpClientFactory clientFactory, SingletonServices settings) : base(clientFactory)
        {
            this.clientFactory = clientFactory;
            _settings = settings;
        }
        public async Task<T> Create<T>(DataModelParameters modelParameters)
        {
            string url = SD.DataModelAPIBase.BuildFunctionUrl($"/api/Create", "", SD.DataModelKey);
            Console.WriteLine($"CreateDataSourceAsync: url = {url}");
            return await this.SendAsync<T>(new ApiRequest()
            {
                ApiType = SD.ApiType.POST,
                AzureStorage = _settings.AzureStorage,
                Url = url,
                Data = modelParameters
            });
        }
    }
}
