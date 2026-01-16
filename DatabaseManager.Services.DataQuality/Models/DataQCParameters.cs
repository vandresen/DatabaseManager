using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseManager.Services.DataQuality.Models
{
    public class DataQCParameters
    {
        public string AzureStorageKey { get; set; }
        public string DataConnector { get; set; }
        public string IndexProject { get; set; }
        public int RuleId { get; set; }
    }
}
