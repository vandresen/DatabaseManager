using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Shared
{
    public class SyncParameters
    {
        public string SourceName { get; set; }
        public string TargetName { get; set; }
        public string DataObjectType { get; set; }
        public bool Remote { get; set; }
    }
}
