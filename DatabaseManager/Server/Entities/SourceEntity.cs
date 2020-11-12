using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;

namespace DatabaseManager.Server.Entities
{
    public class SourceEntity : TableEntity
    {
        public SourceEntity(string sourceName)
        {
            this.PartitionKey = "PPDM"; this.RowKey = sourceName;
        }

        public SourceEntity() { }
        public string SourceType { get; set; }
        public string DatabaseName { get; set; }
        public string DatabaseServer { get; set; }
        public string Password { get; set; }
        public string User { get; set; }
        public string ConnectionString { get; set; }
        public string DataType { get; set; }
        public string FileShare { get; set; }
        public string FileName { get; set; }
    }
}
