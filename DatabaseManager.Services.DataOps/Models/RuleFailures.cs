using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.DataOps.Models
{
    public class RuleFailures
    {
        public int RuleId { get; set; }
        public List<int> Failures { get; set; }
    }
}
