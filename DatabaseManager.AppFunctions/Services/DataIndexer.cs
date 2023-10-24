using DatabaseManager.AppFunctions.Entities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.AppFunctions.Services
{
    public class DataIndexer : IDataIndexer
    {
        private readonly HttpClient _httpClient;

        public DataIndexer(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<Response> Create(BuildIndexParameters iParameters, string url)
        {
            Response response = new Response();
            string json = JsonConvert.SerializeObject(iParameters);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            HttpResponseMessage httpResponse = await _httpClient.PostAsync(url, content);
            if (httpResponse.IsSuccessStatusCode)
            {
                string responseContent = await httpResponse.Content.ReadAsStringAsync();
                response = JsonConvert.DeserializeObject<Response>(responseContent);
            }
            else
            {
                response.DisplayMessage = "Error";
                response.ErrorMessages = new List<string> { "Create: Request failed with status code: " + httpResponse.StatusCode };
                response.IsSuccess = false;
            }
            return response;
        }
    }
}
