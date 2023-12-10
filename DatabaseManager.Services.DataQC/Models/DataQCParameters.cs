using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.DataQC.Models
{
    public class DataQCParameters
    {
        public string AzureStorageKey { get; set; }
        public string DataConnector { get; set; }
        public int RuleId { get; set; }
    }
}
