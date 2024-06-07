namespace DatabaseManager.ServerLessClient.Models
{
    public class PredictionParameters
    {
        public string DataConnector { get; set; }
        public string TargetConnector { get; set; }
        public string DataAccessDefinitions { get; set; }
        public int PredictionId { get; set; }
    }
}
