using Blazored.LocalStorage;
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
        private readonly ILocalStorageService localStorage;

        public DataOpsServerLess(IHttpService httpService, ILocalStorageService localStorage)
        {
            this.httpService = httpService;
            this.localStorage = localStorage;
        }

        public async Task<List<DataOpsPipes>> GetPipelines()
        {
            string baseUrl = await localStorage.GetItemAsync<string>("BaseUrl");
            string url = baseUrl + "GetDataOpsList";
            var response = await httpService.Get<List<DataOpsPipes>>(url);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
            return response.Response;
        }

        public async Task CreatePipeline(DataOpsPipes pipe)
        {
            string baseUrl = await localStorage.GetItemAsync<string>("BaseUrl");
            string url = baseUrl + "SavePipeline";
            Console.WriteLine(url);
            var response = await httpService.Post(url, pipe);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
        }

        public async Task DeletePipeline(string name)
        {
            string baseUrl = await localStorage.GetItemAsync<string>("BaseUrl");
            string url = baseUrl + "DeletePipeline";
            var response = await httpService.Delete($"{url}?name={name}");
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
        }
    }
}
