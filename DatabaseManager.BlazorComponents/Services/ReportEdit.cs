using DatabaseManager.BlazorComponents.Extensions;
using DatabaseManager.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.BlazorComponents.Services
{
    public class ReportEdit : IReportEdit
    {
        private readonly IHttpService httpService;
        private string url;
        private string baseUrl;
        private readonly string apiKey;
        public ReportEdit(IHttpService httpService, SingletonServices settings)
        {
            this.httpService = httpService;
            baseUrl = settings.BaseUrl;
            apiKey = settings.ApiKey;
        }
        public async Task Delete(string source, int id)
        {
            if (string.IsNullOrEmpty(baseUrl)) url = $"api/ReportEdit/{source}/{id}";
            else url = baseUrl.BuildFunctionUrl("DeleteReportData", $"name={source}&id={id}", apiKey);
            Console.WriteLine(url);
            var response = await httpService.Delete(url);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
        }

        public async Task Insert(string source, ReportData reportData)
        {
            throw new NotImplementedException();

        }

        public async Task Update(string source, ReportData reportData)
        {
            if (string.IsNullOrEmpty(baseUrl)) url = $"api/ReportEdit/{source}";
            else url = baseUrl.BuildFunctionUrl("UpdateReportData", $"name={source}", apiKey);
            Console.WriteLine(url);
            var response = await httpService.Put(url, reportData);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
        }

        public async Task<AttributeInfo> GetAttributeInfo(string source, string dataType)
        {
            if (string.IsNullOrEmpty(baseUrl)) url = $"api/ReportEdit/{source}/{dataType}";
            else url = baseUrl.BuildFunctionUrl("ReportAttributeInfo", $"name={source}&datatype={dataType}", apiKey);
            var response = await httpService.Get<AttributeInfo>(url);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
            return response.Response;
        }

        public async Task Merge(string source, ReportData reportData)
        {
            if (string.IsNullOrEmpty(baseUrl)) url = $"api/ReportEdit/merge/{source}";
            else url = baseUrl.BuildFunctionUrl("MergeReportData", $"name={source}", apiKey);
            Console.WriteLine(url);
            var response = await httpService.Put(url, reportData);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
        }

        public async Task InsertChild(string source, ReportData reportData)
        {
            if (string.IsNullOrEmpty(baseUrl)) url = $"api/ReportEdit/{source}";
            else url = baseUrl.BuildFunctionUrl("InsertChildReportData", $"name={source}", apiKey);
            Console.WriteLine(url);
            var response = await httpService.Post(url, reportData);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
        }
    }
}
