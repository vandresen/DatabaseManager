using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseManager.Shared
{
    public class RuleFailures
    {
        public int RuleId { get; set; }
        public List<int> Failures { get; set; }
    }
}
