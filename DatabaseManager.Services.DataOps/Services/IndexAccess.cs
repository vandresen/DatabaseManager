﻿using DatabaseManager.Services.DataOps.Extensions;
using DatabaseManager.Services.DataOps.Models;
using Microsoft.Extensions.Configuration;

namespace DatabaseManager.Services.DataOps.Services
{
    public class IndexAccess : BaseService, IIndexAccess
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly IConfiguration _configuration;

        public IndexAccess(IHttpClientFactory clientFactory,
            IConfiguration configuration) : base(clientFactory)
        {
            _clientFactory = clientFactory;
            _configuration = configuration;
        }

        public async Task<T> GetIndexes<T>(string dataSource, string project, string dataType)
        {
            var indexAPIBase = _configuration.GetValue<string>("IndexAPI");
            var indexKey = _configuration.GetValue<string>("IndexKey");
            string url = indexAPIBase.BuildFunctionUrl($"/QueryIndex", $"Name={dataSource}&DataType={dataType}&Project={project}", indexKey);
            return await this.SendAsync<T>(new ApiRequest()
            {
                ApiType = SD.ApiType.GET,
                AzureStorage = SD.AzureStorageKey,
                Url = url
            });
        }
    }
}
