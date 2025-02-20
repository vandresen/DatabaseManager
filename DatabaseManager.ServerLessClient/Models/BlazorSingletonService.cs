namespace DatabaseManager.ServerLessClient.Models
{
    public class BlazorSingletonService
    {
        public string BaseUrl { get; set; }
        public string AzureStorage { get; set; }
        public string TargetConnector { get; set; }
        public string DataAccessDefinition { get; set; }
        public string ApiKey { get; set; }
        public bool ServerLess { get; set; }
        public int HttpTimeOut = 500;
        public string Project { get; set; }
        public string ReportApiBase { get; set; }
        public string ReportKey { get; set; }
    }
}
