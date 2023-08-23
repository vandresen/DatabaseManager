namespace DatabaseManager.Services.IndexSqlite.Models
{
    public class BuildIndexParameters
    {
        public string StorageAccount { get; set; }
        public string SourceName { get; set; }
        public string TargetName { get; set; }
        public string Taxonomy { get; set; }
        public string Filter { get; set; }
    }
}
