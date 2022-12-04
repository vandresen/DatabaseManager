using static DatabaseManager.BlazorComponents.SD;

namespace DatabaseManager.ServerLessClient.Models
{
    public class ApiRequest
    {
        public ApiType ApiType { get; set; } = ApiType.GET;
        public string Url { get; set; }
        public string AzureStorage { get; set; }
        public object Data { get; set; }
    }
}
