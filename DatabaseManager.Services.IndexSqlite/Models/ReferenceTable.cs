namespace DatabaseManager.Services.IndexSqlite.Models
{
    public class ReferenceTable
    {
        public string DataType { get; set; }
        public string Table { get; set; }
        public string KeyAttribute { get; set; }
        public string ValueAttribute { get; set; }
        public string ReferenceAttribute { get; set; }
        public string FixedKey { get; set; }
        public bool Insert { get; set; }
    }
}
