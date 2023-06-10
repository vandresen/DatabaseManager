using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseManager.Shared
{
    public class DataQCParameters
    {
        public string DataConnector { get; set; }
        public int RuleId { get; set; }
        public bool ClearQCFlags { get; set; }
        public List<int> Failures { get; set; }
    }
}
