using Blazored.LocalStorage;
using DatabaseManager.Common.Extensions;
using DatabaseManager.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Common.Services
{
    public class DataOpsServerLess: IDataOps
    {
        private readonly IHttpService httpService;
        private readonly string baseUrl;
        private readonly string apiKey;

        public DataOpsServerLess(IHttpService httpService, SingletonService settings)
        {
            this.httpService = httpService;
            baseUrl = settings.BaseUrl;
            apiKey = settings.ApiKey;
        }

        public async Task<List<DataOpsPipes>> GetPipelines()
        {
            //string baseUrl = await localStorage.GetItemAsync<string>("BaseUrl");
            string url = baseUrl.BuildFunctionUrl("GetDataOpsList", $"", apiKey);
            //string url = baseUrl + "GetDataOpsList";
            Console.WriteLine($"Url = {url}");
            var response = await httpService.Get<List<DataOpsPipes>>(url);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
            return response.Response;
        }

        public async Task<List<PipeLine>> GetPipeline(string name)
        {
            //string baseUrl = await localStorage.GetItemAsync<string>("BaseUrl");
            //string url = baseUrl + "GetDataOpsPipe" + $"?name={name}";
            string url = baseUrl.BuildFunctionUrl("GetDataOpsPipe", $"name={name}", apiKey);
            Console.WriteLine(url);
            var response = await httpService.Get<List<PipeLine>>(url);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
            return response.Response;
        }

        public async Task CreatePipeline(DataOpsPipes pipe)
        {
            //string baseUrl = await localStorage.GetItemAsync<string>("BaseUrl");
            //string url = baseUrl + "SavePipeline";
            string url = baseUrl.BuildFunctionUrl("SavePipeline", $"", apiKey);
            Console.WriteLine($"Url = {url}");
            var response = await httpService.Post(url, pipe);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
        }

        public async Task ProcessPipeline(List<DataOpParameters> parms)
        {
            //string baseUrl = await localStorage.GetItemAsync<string>("BaseUrl");
            //string url = baseUrl + "ManageDataOps_HttpStart";
            string url = baseUrl.BuildFunctionUrl("ManageDataOps_HttpStart", $"", apiKey);
            Console.WriteLine($"Url = {url}");
            var response = await httpService.Post(url, parms);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
        }

        public async Task DeletePipeline(string name)
        {
            //string baseUrl = await localStorage.GetItemAsync<string>("BaseUrl");
            //string url = baseUrl + "DeletePipeline";
            string url = baseUrl.BuildFunctionUrl("DeletePipeline", $"name={name}", apiKey);
            Console.WriteLine($"Url = {url}");
            var response = await httpService.Delete(url);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
        }

        public async Task SavePipeline(DataOpsPipes pipe, List<PipeLine> tubes)
        {
            string name = pipe.Name;
            //string baseUrl = await localStorage.GetItemAsync<string>("BaseUrl");
            //string url = baseUrl + "SavePipelineData" + $"?name={name}";
            string url = baseUrl.BuildFunctionUrl("SavePipelineData", $"name={name}", apiKey);
            Console.WriteLine($"Url = {url}");
            var response = await httpService.Post(url, tubes);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
        }
    }
}
