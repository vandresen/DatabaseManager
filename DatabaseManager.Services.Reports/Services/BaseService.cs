using DatabaseManager.Services.Reports.Models;
using Newtonsoft.Json;
using System.Text;

namespace DatabaseManager.Services.Reports.Services
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
                var client = httpClient.CreateClient("DatabaseManagerAPI");
                HttpRequestMessage message = new HttpRequestMessage();
                message.Headers.Add("Accept", "application/json");

                // ─── START AUTOMATIC KEY STRIPPING & HEADER SHIFT ───
                var uriBuilder = new UriBuilder(apiRequest.Url);
                var queryParams = System.Web.HttpUtility.ParseQueryString(uriBuilder.Query);

                // Automatically find the Azure function key parameter from ANY of your 10 services
                string functionKey = queryParams["code"];

                if (!string.IsNullOrEmpty(functionKey))
                {
                    // Strip the secret out of the query collection completely
                    queryParams.Remove("code");

                    // Re-apply the clean parameters to the URI builder
                    uriBuilder.Query = queryParams.ToString();

                    // Safely move the key to the background HTTP Header
                    message.Headers.Add("x-functions-key", functionKey);
                }

                // Assign the sanitized, secure URL to the request message
                message.RequestUri = uriBuilder.Uri;
                // ─── END AUTOMATIC KEY STRIPPING & HEADER SHIFT ───

                client.DefaultRequestHeaders.Clear();
                if (apiRequest.Data != null)
                {
                    message.Content = new StringContent(JsonConvert.SerializeObject(apiRequest.Data),
                        Encoding.UTF8, "application/json");
                }

                HttpResponseMessage apiResponse = null;
                switch (apiRequest.ApiType)
                {
                    case SD.ApiType.POST:
                        message.Method = HttpMethod.Post;
                        break;
                    case SD.ApiType.PUT:
                        message.Method = HttpMethod.Put;
                        break;
                    case SD.ApiType.DELETE:
                        message.Method = HttpMethod.Delete;
                        break;
                    default:
                        message.Method = HttpMethod.Get;
                        break;
                }
                if (!string.IsNullOrEmpty(apiRequest.AzureStorage))
                {
                    client.DefaultRequestHeaders.Add("azurestorageconnection", apiRequest.AzureStorage);
                }
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
