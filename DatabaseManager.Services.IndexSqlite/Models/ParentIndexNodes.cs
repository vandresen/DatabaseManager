namespace DatabaseManager.Services.IndexSqlite.Models
{
    public class ParentIndexNodes
    {
        public string Name { get; set; }
        public int NodeCount { get; set; }
        public string ParentNodeId { get; set; }
        public int ParentId { get; set; }
    }
}
