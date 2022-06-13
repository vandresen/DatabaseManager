using DatabaseManager.BlazorComponents.Extensions;
using DatabaseManager.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.BlazorComponents.Services
{
    public class DataQc : IDataQc
    {
        private readonly IHttpService httpService;
        private string url;
        private string baseUrl;
        private readonly string apiKey;

        public DataQc(IHttpService httpService, SingletonServices settings)
        {
            this.httpService = httpService;
            baseUrl = settings.BaseUrl;
            apiKey = settings.ApiKey;
        }

        public Task ClearQCFlags(string source)
        {
            throw new NotImplementedException();
        }

        public async Task<List<DmsIndex>> GetQcFailures(string source, int id)
        {
            if (string.IsNullOrEmpty(baseUrl)) url = $"api/DataQC/{source}/{id}";
            else url = baseUrl.BuildFunctionUrl("GetDataQcResults", $"name={source}&id={id}", apiKey);
            var response = await httpService.Get<List<DmsIndex>>(url);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
            return response.Response;
        }

        public async Task<List<QcResult>> GetQcResult(string source)
        {
            if (string.IsNullOrEmpty(baseUrl)) url = $"api/DataQC/{source}";
            else url = baseUrl.BuildFunctionUrl("GetDataQcResults", $"name={source}", apiKey);
            var response = await httpService.Get<List<QcResult>>(url);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
            return response.Response;
        }

        public Task ProcessQCRule(DataQCParameters qcParams)
        {
            throw new NotImplementedException();
        }

        public async Task<List<QcResult>> GetResults(string source)
        {
            if (string.IsNullOrEmpty(baseUrl)) url = $"api/dataqc/{source}";
            else url = baseUrl.BuildFunctionUrl("GetResults", $"name={source}", apiKey);
            Console.WriteLine(url);
            var response = await httpService.Get<List<QcResult>>(url);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
            return response.Response;
        }

        public async Task<List<DmsIndex>> GetResult(string source, int id)
        {
            if (string.IsNullOrEmpty(baseUrl)) url = $"api/DataQC/{source}/{id}";
            else url = baseUrl.BuildFunctionUrl("GetResult", $"name={source}&id={id}", apiKey);
            var response = await httpService.Get<List<DmsIndex>>(url);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
            return response.Response;
        }
    }
}
