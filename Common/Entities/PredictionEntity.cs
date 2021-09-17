using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseManager.Common.Entities
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
