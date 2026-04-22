namespace DatabaseManager.ServerLessClient.Models
{
    public class PredictionParameters
    {
        public string DataConnector { get; set; }
        public string TargetConnector { get; set; }
        public string AzureStorageKey { get; set; }
        public int IndexProject { get; set; }
    }
}
