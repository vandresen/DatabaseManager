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
        public Task Delete(string source, int id)
        {
            throw new NotImplementedException();
        }

        public async Task Insert(string source, ReportData reportData)
        {
            throw new NotImplementedException();

        }

        public async Task Update(string source, ReportData reportData)
        {
            if (string.IsNullOrEmpty(baseUrl)) url = $"api/ReportEdit/{source}";
            else url = baseUrl.BuildFunctionUrl("UpdateReportData", $"name={source}", apiKey);
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
    }
}
