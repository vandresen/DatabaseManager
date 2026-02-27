using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.Predictions.Models
{
    public class PredictionParameters
    {
        public string DataConnector { get; set; }
        //public string TargetConnector { get; set; }
        //public string DataAccessDefinitions { get; set; }
        public int PredictionId { get; set; }
        public string AzureStorageKey { get; set; }
        public string IndexProject { get; set; }
    }
}
