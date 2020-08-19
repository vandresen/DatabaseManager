using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DatabaseManager.Client.Helpers
{
    public class SingletonServices
    {
        public string TargetConnector { get; set; }
        public string DataAccessDefinition { get; set; }
    }
}
