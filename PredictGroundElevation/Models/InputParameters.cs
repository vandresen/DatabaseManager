using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PredictGroundElevation.Models
{
    public class InputParameters
    {
        public string Database { get; set; }

        public string DatabaseServer { get; set; }

        public string DatabaseUser { get; set; }

        public string DatabasePassword { get; set; }
        public string DataConnector { get; set; }

        public string DataObject { get; set; }

        public string RuleObject { get; set; }

        public int IndexId { get; set; }

        public string IndexNode { get; set; }
        public string ConsistencyConnectorString { get; set; }
    }
}
