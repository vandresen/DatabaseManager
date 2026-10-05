namespace DatabaseManager.Services.IndexSqlite.Models
{
    public class FailureCountDto
    {
        public string DataType { get; set; }
        public string RuleKey { get; set; }
        public int Count { get; set; }
    }
}
