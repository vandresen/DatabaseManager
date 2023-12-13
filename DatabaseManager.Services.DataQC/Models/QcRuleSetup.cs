using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.DataQC.Models
{
    public class QcRuleSetup
    {
        //public string DataConnector { get; set; }

        //public string DataObject { get; set; }

        public string RuleObject { get; set; }

        public int IndexId { get; set; }
        public int EniretyIndexId { get; set; }

        public string ConsistencyConnectorString { get; set; }
    }
}
