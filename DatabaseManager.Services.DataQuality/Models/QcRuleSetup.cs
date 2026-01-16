namespace DatabaseManager.Services.DataQuality.Models
{
    public class QcRuleSetup
    {
        public string RuleObject { get; set; }

        public int IndexId { get; set; }
        public int EniretyIndexId { get; set; }

        public string ConsistencyConnectorString { get; set; }
    }
}
