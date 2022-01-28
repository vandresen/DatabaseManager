using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseManager.Shared
{
    public class DataQCDataOpsCloseParameters
    {
        public DataOpParameters Parameters { get; set; }
        public List<RuleFailures> Failures { get; set; }
    }
}
