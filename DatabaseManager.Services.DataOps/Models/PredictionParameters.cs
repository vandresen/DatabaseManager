namespace DatabaseManager.Services.DataOps.Models
{
    public class PredictionParameters
    {
        public string DataConnector { get; set; }
        public int PredictionId { get; set; }
        public string AzureStorageKey { get; set; }
        public string IndexProject { get; set; }
        public DatabaseProvider DatabaseProvider { get; set; }
    }
}
