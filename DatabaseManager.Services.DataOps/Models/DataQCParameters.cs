namespace DatabaseManager.Services.DataOps.Models
{
    public class DataQCParameters
    {
        public string AzureStorageKey { get; set; }
        public string DataConnector { get; set; }
        public string IndexProject { get; set; }
        public int RuleId { get; set; }
        public DatabaseProvider DatabaseProvider { get; set; }
    }
}
