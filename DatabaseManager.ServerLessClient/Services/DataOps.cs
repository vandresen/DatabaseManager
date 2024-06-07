using DatabaseManager.ServerLessClient.Models;
using DatabaseManager.ServerLessClient.Helpers;

namespace DatabaseManager.ServerLessClient.Services
{
    public class DataOps: BaseService, IDataOps
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly BlazorSingletonService _settings;

        public DataOps(IHttpClientFactory clientFactory, 
            BlazorSingletonService settings) : base(clientFactory)
        {
            _clientFactory = clientFactory;
            _settings = settings;
        }

        public async Task<T> GetPipelines<T>()
        {
            List<DataOpsPipes> results = new List<DataOpsPipes>();
            string url = SD.DataOpsManageAPIBase.BuildFunctionUrl($"/api/GetDataOpsList", "", SD.DataOpsManageKey);
            return await SendAsync<T>(new ApiRequest()
            {
                ApiType = SD.ApiType.GET,
                AzureStorage = _settings.AzureStorage,
                Url = url
            });
        }

        public Task<DataOpsStatus> GetStatus(string url)
        {
            throw new NotImplementedException();
        }

        public Task ProcessPipeline(List<DataOpParameters> parms)
        {
            throw new NotImplementedException();
        }

        public Task<DataOpsResults> ProcessPipelineWithStatus(List<DataOpParameters> parms)
        {
            throw new NotImplementedException();
        }

        public Task SavePipeline(DataOpsPipes pipe, List<PipeLine> tubes)
        {
            throw new NotImplementedException();
        }

        Task IDataOps.CreatePipeline(DataOpsPipes pipe)
        {
            throw new NotImplementedException();
        }

        Task IDataOps.DeletePipeline(string name)
        {
            throw new NotImplementedException();
        }

        Task<List<PipeLine>> IDataOps.GetPipeline(string name)
        {
            throw new NotImplementedException();
        }

        //public async Task<List<PipeLine>> GetPipeline(string name)
        //{
        //    string url = baseUrl.BuildFunctionUrl("GetDataOpsPipe", $"name={name}", apiKey);
        //    Console.WriteLine(url);
        //    var response = await httpService.Get<List<PipeLine>>(url);
        //    if (!response.Success)
        //    {
        //        throw new ApplicationException(await response.GetBody());
        //    }
        //    return response.Response;
        //}

        //public async Task CreatePipeline(DataOpsPipes pipe)
        //{
        //    string url = baseUrl.BuildFunctionUrl("SavePipeline", $"", apiKey);
        //    Console.WriteLine($"Url = {url}");
        //    var response = await httpService.Post(url, pipe);
        //    if (!response.Success)
        //    {
        //        throw new ApplicationException(await response.GetBody());
        //    }
        //}

        //public async Task ProcessPipeline(List<DataOpParameters> parms)
        //{
        //    string url = baseUrl.BuildFunctionUrl("ManageDataOps_HttpStart", $"", apiKey);
        //    Console.WriteLine($"Url = {url}");
        //    var response = await httpService.Post(url, parms);
        //    if (!response.Success)
        //    {
        //        throw new ApplicationException(await response.GetBody());
        //    }
        //}

        //public async Task DeletePipeline(string name)
        //{
        //    string url = baseUrl.BuildFunctionUrl("DeletePipeline", $"name={name}", apiKey);
        //    Console.WriteLine($"Url = {url}");
        //    var response = await httpService.Delete(url);
        //    if (!response.Success)
        //    {
        //        throw new ApplicationException(await response.GetBody());
        //    }
        //}

        //public async Task SavePipeline(DataOpsPipes pipe, List<PipeLine> tubes)
        //{
        //    string name = pipe.Name;
        //    string url = baseUrl.BuildFunctionUrl("SavePipelineData", $"name={name}", apiKey);
        //    Console.WriteLine($"Url = {url}");
        //    var response = await httpService.Post(url, tubes);
        //    if (!response.Success)
        //    {
        //        throw new ApplicationException(await response.GetBody());
        //    }
        //}

        //public async Task<DataOpsResults> ProcessPipelineWithStatus(List<DataOpParameters> parms)
        //{
        //    string url = baseUrl.BuildFunctionUrl("ManageDataOps_HttpStart", $"", apiKey);
        //    Console.WriteLine($"Url = {url}");
        //    var response = await httpService.Post<List<DataOpParameters>, DataOpsResults>(url, parms);
        //    if (!response.Success)
        //    {
        //        throw new ApplicationException(await response.GetBody());
        //    }
        //    return response.Response;
        //}

        //public async Task<DataOpsStatus> GetStatus(string url)
        //{
        //    var response = await httpService.Get<DataOpsStatus>(url);
        //    if (!response.Success)
        //    {
        //        throw new ApplicationException(await response.GetBody());
        //    }
        //    return response.Response;
        //}
    }
}
