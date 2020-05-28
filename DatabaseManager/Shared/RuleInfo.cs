using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseManager.Shared
{
    public class RuleInfo
    {
        public List<string> DataTypeOptions { get; set; }
        public Dictionary<string, string> DataAttributes { get; set; }
    }
}
