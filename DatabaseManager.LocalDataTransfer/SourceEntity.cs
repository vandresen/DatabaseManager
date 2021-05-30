using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.LocalDataTransfer
{
    public class SourceEntity : TableEntity
    {
        public SourceEntity(string sourceName)
        {
            this.PartitionKey = "PPDM"; this.RowKey = sourceName;
        }

        public SourceEntity() { }
        public string SourceType { get; set; }
        public string Catalog { get; set; }
        public string DatabaseServer { get; set; }
        public string Password { get; set; }
        public string User { get; set; }
        public string ConnectionString { get; set; }
        public string DataType { get; set; }
        public string FileName { get; set; }
        public int CommandTimeOut { get; set; }
    }
}
