using DatabaseManager.AppFunctions.Entities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using DatabaseManager.AppFunctions.Extensions;
using Microsoft.AspNetCore.Http;
using System.Text;

namespace DatabaseManager.AppFunctions.Services
{
    public class DataTransferService : IDataTransferService
    {
        private readonly HttpClient _httpClient;

        public DataTransferService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        public async Task<Response> Copy(TransferParameters transferParameters, ApiInfo apiInfo, string azureStorage)
        {
            string url = "";
            string baseUrl = apiInfo.BaseUrl;
            Response response = new Response();
            if (transferParameters.SourceType == "DataBase")
            {
                url = baseUrl.BuildFunctionUrl("/api/CopyDatabaseObject", $"", apiInfo.ApiKey);
            }
            else if (transferParameters.SourceType == "File")
            {
                if (transferParameters.SourceDataType == "Logs")
                {
                    url = baseUrl.BuildFunctionUrl("/api/CopyLASObject", $"", apiInfo.ApiKey);
                }
                else
                {
                    url = baseUrl.BuildFunctionUrl("/api/CopyCSVObject", $"", apiInfo.ApiKey);
                }
            }
            else
            {
                response.DisplayMessage = "Error";
                response.ErrorMessages = new List<string> { "Copy: Bad source type "};
                response.IsSuccess = false;
                return response;
            }
            string json = JsonConvert.SerializeObject(transferParameters);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            _httpClient.DefaultRequestHeaders.Add("azurestorageconnection", azureStorage);
            HttpResponseMessage httpResponse = await _httpClient.PostAsync(url, content);
            if (httpResponse.IsSuccessStatusCode)
            {
                string responseContent = await httpResponse.Content.ReadAsStringAsync();
                response = JsonConvert.DeserializeObject<Response>(responseContent);
            }
            else
            {
                response.DisplayMessage = "Error";
                response.ErrorMessages = new List<string> { "Copy: Request failed with status code: " + httpResponse.StatusCode };
                response.IsSuccess = false;
            }
            return response;
        }

        public Task CopyRemote(TransferParameters transferParameters)
        {
            throw new NotImplementedException();
        }

        public async Task<Response> DeleteTable(string url, string azureStorage)
        {
            Response response = new Response();
            _httpClient.DefaultRequestHeaders.Add("azurestorageconnection", azureStorage);
            HttpResponseMessage httpResponse = await _httpClient.DeleteAsync(url);
            if (httpResponse.IsSuccessStatusCode)
            {
                string responseContent = await httpResponse.Content.ReadAsStringAsync();
                response = JsonConvert.DeserializeObject<Response>(responseContent);
            }
            else
            {
                response.DisplayMessage = "Error";
                response.ErrorMessages = new List<string> { "GetDataObjects: Request failed with status code: " + httpResponse.StatusCode };
                response.IsSuccess = false;
            }
            return response;
        }

        public async Task<Response> GetDataObjects(string url, string azureStorage)
        {
            Response response = new Response();
            List<string> dataObjects = new List<string>();
            _httpClient.DefaultRequestHeaders.Add("azurestorageconnection", azureStorage);
            HttpResponseMessage httpResponse = await _httpClient.GetAsync(url);
            if (httpResponse.IsSuccessStatusCode)
            {
                string responseContent = await httpResponse.Content.ReadAsStringAsync();
                response = JsonConvert.DeserializeObject<Response>(responseContent);
            }
            else
            {
                response.DisplayMessage = "Error";
                response.ErrorMessages = new List<string> { "GetDataObjects: Request failed with status code: " + httpResponse.StatusCode };
                response.IsSuccess = false;
            }
            return response;
        }

        public Task<List<MessageQueueInfo>> GetQueueMessage()
        {
            throw new NotImplementedException();
        }
    }
}
