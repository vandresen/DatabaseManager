﻿using DatabaseManager.ServerLessClient.Models;
using DatabaseManager.ServerLessClient.Helpers;
using Newtonsoft.Json;

namespace DatabaseManager.ServerLessClient.Services
{
    public class ReportService : BaseService, IReport
    {
        string baseUrl;
        string apiKey;
        private readonly IHttpClientFactory _clientFactory;

        public ReportService(IHttpClientFactory clientFactory, IConfiguration configuration) : base(clientFactory)
        {
            baseUrl = configuration["ServiceUrls:ReportApiBase"];
            apiKey = configuration["ServiceUrls: ReportKey"];
        }

        public async Task<List<QcResult>> GetResults(string source)
        {
            List<QcResult> result = new List<QcResult>();
            string url = baseUrl.BuildFunctionUrl("GetResults", $"name={source}", apiKey);
            Console.WriteLine(url);
            ResponseDto response = await this.SendAsync<ResponseDto>(new ApiRequest()
            {
                ApiType = SD.ApiType.GET,
                Url = url
            });
            result = JsonConvert.DeserializeObject<List<QcResult>>(response.Result.ToString());
            return result;
        }

        public async Task<List<TableSchema>> GetAttributeInfo(string source, string dataType)
        {
            List<TableSchema> result = new List<TableSchema>();
            string url = baseUrl.BuildFunctionUrl("ReportAttributeInfo", $"Name={source}&ReportAttributeInfo={dataType}", apiKey);
            Console.WriteLine(url);
            ResponseDto response = await this.SendAsync<ResponseDto>(new ApiRequest()
            {
                ApiType = SD.ApiType.GET,
                Url = url
            });
            result = JsonConvert.DeserializeObject<List<TableSchema>>(response.Result.ToString());
            return result;
        }
    }
}
