namespace DatabaseManager.Services.Rules.Models
{
    public class PredictionSet
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string RuleUrl { get; set; }
        public List<RuleModel> RuleSet { get; set; }
    }
}
