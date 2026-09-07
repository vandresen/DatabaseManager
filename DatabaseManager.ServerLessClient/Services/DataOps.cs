using DatabaseManager.ServerLessClient.Helpers;
using DatabaseManager.ServerLessClient.Models;
using System.Net.Http.Json;

namespace DatabaseManager.ServerLessClient.Services
{
    public class DataOps: BaseService, IDataOps
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly BlazorSingletonService _settings;

        private string resultMessage;

        public DataOps(IHttpClientFactory clientFactory, 
            BlazorSingletonService settings, IConfiguration configuration) : base(clientFactory)
        {
            _clientFactory = clientFactory;
            _settings = settings;
        }

        public async Task<T> GetPipelines<T>()
        {
            List<DataOpsPipes> results = new List<DataOpsPipes>();
            string url = _settings.DataOpsManageAPI.BuildFunctionUrl($"/api/GetDataOpsList", "", _settings.DataOpsManageKey);
            Console.WriteLine($"GetPipelines: url = {url}");
            Console.WriteLine($"GetPipelines: AzureStorage = {_settings.AzureStorage}");
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
            string url = _settings.DataOpsAPI.BuildFunctionUrl($"api/DataOps_HttpStart", "", _settings.DataOpsKey);

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

        public async Task<T> SavePipeline<T>(DataOpsPipes pipe, List<PipeLine> tubes)
        {
            string name = pipe.Name;
            string url = _settings.DataOpsManageAPI.BuildFunctionUrl($"/api/SavePipeline", $"Name={name}", _settings.DataOpsManageKey);
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
            string url = _settings.DataOpsManageAPI.BuildFunctionUrl($"/api/SavePipeline", $"Name={name}", _settings.DataOpsManageKey);
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
            string url = _settings.DataOpsManageAPI.BuildFunctionUrl($"/api/DeletePipeline", $"Name={name}", _settings.DataOpsManageKey);
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
            string url = _settings.DataOpsManageAPI.BuildFunctionUrl($"/api/GetPipe", $"Name={name}", _settings.DataOpsManageKey);
            Console.WriteLine($"GetPipe: url = {url}");
            return await SendAsync<T>(new ApiRequest()
            {
                ApiType = SD.ApiType.GET,
                AzureStorage = _settings.AzureStorage,
                Url = url
            });
        }

    }
}
