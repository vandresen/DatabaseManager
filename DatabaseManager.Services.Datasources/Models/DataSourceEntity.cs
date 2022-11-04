using Azure;
using Azure.Data.Tables;
using System;

namespace DatabaseManager.Services.Datasources.Models
{
    public class DataSourceEntity : ITableEntity
    {
        public string Catalog { get; set; }
        public string ConnectionString { get; set; }
        public string DatabaseServer { get; set; }
        public string Password { get; set; }
        public string SourceType { get; set; }
        public string User { get; set; }
        public string DataType { get; set; }
        public string FileName { get; set; }
        public int CommandTimeOut { get; set; }
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}
