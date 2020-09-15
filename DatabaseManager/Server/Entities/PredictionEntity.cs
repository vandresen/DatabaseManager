using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DatabaseManager.Server.Entities
{
    public class PredictionEntity : TableEntity
    {
        public PredictionEntity(string name)
        {
            this.PartitionKey = "PPDM"; this.RowKey = name;
        }

        public PredictionEntity() { }
        public string RuleUrl { get; set; }
        public string Decsription { get; set; }
    }
}
