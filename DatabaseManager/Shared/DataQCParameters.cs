using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseManager.Shared
{
    public class DataQCParameters
    {
        public string DataConnector { get; set; }
        public string DataAccessDefinitions { get; set; }
        public int RuleId { get; set; }
        public bool ClearQCFlags { get; set; }
    }
}
