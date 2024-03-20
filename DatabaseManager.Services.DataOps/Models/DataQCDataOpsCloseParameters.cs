using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.DataOps.Models
{
    public class DataQCDataOpsCloseParameters
    {
        public DataOpParameters Parameters { get; set; }
        public List<RuleFailures> Failures { get; set; }
    }
}
