﻿using DatabaseManager.Common.Extensions;
using DatabaseManager.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Common.Services
{
    public class IndexView : IIndexView
    {
        private readonly IHttpService httpService;
        private string url;
        private string baseUrl;
        private string apiKey;

        public IndexView(IHttpService httpService, SingletonService settings)
        {
            this.httpService = httpService;
            baseUrl = settings.BaseUrl;
            apiKey = settings.ApiKey;
        }

        public async Task<List<DmsIndex>> GetChildren(string source, int id)
        {
            if (string.IsNullOrEmpty(baseUrl)) url = $"api/index/{source}/{id}";
            else url = baseUrl.BuildFunctionUrl("GetIndexItem", $"name={source}&id={id}", apiKey);
            var response = await httpService.Get<List<DmsIndex>>(url);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
            return response.Response;
        }

        public async Task<List<DmsIndex>> GetIndex(string source)
        {
            if (string.IsNullOrEmpty(baseUrl)) url = $"api/index/{source}";
            else url = baseUrl.BuildFunctionUrl("GetIndexData", $"name={source}", apiKey);
            var response = await httpService.Get<List<DmsIndex>>(url);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
            return response.Response;
        }

        public void InitSettings(SingletonService settings)
        {
            baseUrl = settings.BaseUrl;
            apiKey = settings.ApiKey;
        }
    }
}