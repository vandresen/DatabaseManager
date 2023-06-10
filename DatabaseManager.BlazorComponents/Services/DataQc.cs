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

        public async Task<DataQCParameters> ProcessQCRule(DataQCParameters qcParams)
        {
            if (string.IsNullOrEmpty(baseUrl)) url = "api/DataQC";
            else throw new ApplicationException("URL error");
            Console.WriteLine($"Execure QC URL: {url}");
            var response = await httpService.Post<DataQCParameters, DataQCParameters>(url, qcParams);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
            return response.Response;
        }

        public async Task ClearQCFlags(string source)
        {
            if (string.IsNullOrEmpty(baseUrl)) url = $"api/DataQC/ClearQCFlags/{source}";
            else throw new ApplicationException("URL error");
            Console.WriteLine($"Clear QC flags url: {url}");
            var response = await httpService.Post(url);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
        }

        public async Task CloseQC(string source, List<RuleFailures> failures)
        {
            if (string.IsNullOrEmpty(baseUrl)) url = $"api/DataQC/Close/{source}";
            else throw new ApplicationException("URL error");
            Console.WriteLine($"Close QC url: {url}");
            var response = await httpService.Post(url, failures);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
        }
    }
}
