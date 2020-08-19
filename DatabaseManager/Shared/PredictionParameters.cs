using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseManager.Shared
{
    public class PredictionParameters
    {
        public string DataConnector { get; set; }
        public string DataAccessDefinitions { get; set; }
        public int PredictionId { get; set; }
    }
}
