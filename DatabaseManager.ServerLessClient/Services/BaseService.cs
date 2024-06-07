using Newtonsoft.Json;
using System.Text;
using DatabaseManager.ServerLessClient.Models;

namespace DatabaseManager.ServerLessClient.Services
{
    public class BaseService : IBaseService
    {
        public ResponseDto responseModel { get; set; }
        public IHttpClientFactory httpClient { get; set; }

        public BaseService(IHttpClientFactory httpClient)
        {
            this.responseModel = new ResponseDto();
            this.httpClient = httpClient;
        }

        public async Task<T> SendAsync<T>(ApiRequest apiRequest)
        {
            try
            {
                var httpMethod = new HttpMethod("GET");
                switch (apiRequest.ApiType)
                {
                    case SD.ApiType.POST:
                        httpMethod = HttpMethod.Post;
                        break;
                    case SD.ApiType.PUT:
                        httpMethod = HttpMethod.Put;
                        break;
                    case SD.ApiType.DELETE:
                        httpMethod = HttpMethod.Delete;
                        break;
                }
                var client = httpClient.CreateClient("DatabaseManager");
                HttpRequestMessage message = new HttpRequestMessage(httpMethod, apiRequest.Url);
                message.Headers.Add("Accept", "application/json");
                client.DefaultRequestHeaders.Clear();
                if (apiRequest.Data != null)
                {
                    message.Content = new StringContent(JsonConvert.SerializeObject(apiRequest.Data),
                        Encoding.UTF8, "application/json");
                }

                if (!string.IsNullOrEmpty(apiRequest.AzureStorage))
                {
                    client.DefaultRequestHeaders.Add("azurestorageconnection", apiRequest.AzureStorage);
                }

                HttpResponseMessage apiResponse = null;
                apiResponse = await client.SendAsync(message);

                var apiContent = await apiResponse.Content.ReadAsStringAsync();
                var apiResponseDto = JsonConvert.DeserializeObject<T>(apiContent);
                return apiResponseDto;
            }
            catch (Exception e)
            {
                var dto = new ResponseDto
                {
                    DisplayMessage = "Error",
                    ErrorMessages = new List<string> { Convert.ToString(e.Message) },
                    IsSuccess = false
                };
                var res = JsonConvert.SerializeObject(dto);
                var apiResponseDto = JsonConvert.DeserializeObject<T>(res);
                return apiResponseDto;
            }
        }

        public void Dispose()
        {
            GC.SuppressFinalize(true);
        }
    }
}
