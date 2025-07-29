using DatabaseManager.ServerLessClient.Helpers;
using DatabaseManager.ServerLessClient.Models;
using System.Net.Http.Json;

namespace DatabaseManager.ServerLessClient.Services
{
    public class DataOps: BaseService, IDataOps
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly BlazorSingletonService _settings;
        private readonly string _dataOpsApiUrl;
        private readonly string _dataOpsKey;
        private string resultMessage;

        public DataOps(IHttpClientFactory clientFactory, 
            BlazorSingletonService settings, IConfiguration configuration) : base(clientFactory)
        {
            _clientFactory = clientFactory;
            _settings = settings;
            _dataOpsApiUrl = configuration["ServiceUrls:DataOpsAPI"];
            _dataOpsKey = configuration["ServiceUrls:DataOpsKey"];
        }

        public async Task<T> GetPipelines<T>()
        {
            List<DataOpsPipes> results = new List<DataOpsPipes>();
            string url = SD.DataOpsManageAPIBase.BuildFunctionUrl($"/api/GetDataOpsList", "", SD.DataOpsManageKey);
            Console.WriteLine($"GetPipelines: url = {url}");
            return await SendAsync<T>(new ApiRequest()
            {
                ApiType = SD.ApiType.GET,
                AzureStorage = _settings.AzureStorage,
                Url = url
            });
        }

        public async Task<DataOpsStatus> GetStatus(string url)
        {
            try
            {
                var client = _clientFactory.CreateClient();
                var response = await client.GetFromJsonAsync<DataOpsStatus>(url);
                return response;
            }
            catch (Exception ex)
            {
                resultMessage = $"There was no status content";
                throw new ApplicationException(resultMessage);
            }
        }

        public async Task<DataOpsResults> ProcessPipeline(List<DataOpParameters> parms)
        {
            var client = _clientFactory.CreateClient("DataOpsAPI");
            string url = _dataOpsApiUrl.BuildFunctionUrl($"api/DataOps_HttpStart", "", _dataOpsKey);

            try
            {
                var response = await client.PostAsJsonAsync(url, parms);
                Console.WriteLine($"Status = {response.IsSuccessStatusCode}");
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadFromJsonAsync<DataOpsResults>();
                    if (responseContent != null)
                    {
                        return responseContent;
                    }
                    else
                    {
                        resultMessage = $"There was no content";
                        throw new ApplicationException(resultMessage);
                    }
                }
                else
                {
                    resultMessage = "There was an error sending data.";
                    throw new ApplicationException(resultMessage);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
                throw;
            }

            
            
        }

        public async Task<T> SavePipeline<T>(DataOpsPipes pipe, List<DatabaseManager.Shared.PipeLine> tubes)
        {
            string name = pipe.Name;
            string url = SD.DataOpsManageAPIBase.BuildFunctionUrl($"/api/SavePipeline", $"Name={name}", SD.DataOpsManageKey);
            Console.WriteLine($"GetPipelines: url = {url}");
            return await SendAsync<T>(new ApiRequest()
            {
                ApiType = SD.ApiType.POST,
                AzureStorage = _settings.AzureStorage,
                Url = url,
                Data = tubes
            });
        }

        public async Task<T> CreatePipeline<T>(string name)
        {
            string url = SD.DataOpsManageAPIBase.BuildFunctionUrl($"/api/SavePipeline", $"Name={name}", SD.DataOpsManageKey);
            Console.WriteLine($"DeletePipeline: url = {url}");
            return await this.SendAsync<T>(new ApiRequest()
            {
                ApiType = SD.ApiType.POST,
                AzureStorage = _settings.AzureStorage,
                Url = url
            });
        }

        public async Task<T> DeletePipeline<T>(string name)
        {
            string url = SD.DataOpsManageAPIBase.BuildFunctionUrl($"/api/DeletePipeline", $"Name={name}", SD.DataOpsManageKey);
            Console.WriteLine($"DeletePipeline: url = {url}");
            return await this.SendAsync<T>(new ApiRequest()
            {
                ApiType = SD.ApiType.DELETE,
                AzureStorage = _settings.AzureStorage,
                Url = url
            });
        }

        public async Task<T> GetPipeline<T>(string name)
        {
            List<DataOpsPipes> results = new List<DataOpsPipes>();
            string url = SD.DataOpsManageAPIBase.BuildFunctionUrl($"/api/GetPipe", $"Name={name}", SD.DataOpsManageKey);
            Console.WriteLine($"GetPipe: url = {url}");
            return await SendAsync<T>(new ApiRequest()
            {
                ApiType = SD.ApiType.GET,
                AzureStorage = _settings.AzureStorage,
                Url = url
            });
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
