

using static DatabaseManager.Services.Predictions.SD;

namespace DatabaseManager.Services.Predictions.Models
{
    public class ApiRequest
    {
        public ApiType ApiType { get; set; } = ApiType.GET;
        public string Url { get; set; }
        public string AzureStorage { get; set; }
        public object Data { get; set; }
    }
}
