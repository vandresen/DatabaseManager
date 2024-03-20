using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.DataOps.Models
{
    public class QcResult
    {
        public int Id { get; set; }

        public string RuleName { get; set; }

        public string RuleType { get; set; }

        public string DataType { get; set; }

        public string RuleKey { get; set; }

        public int Failures { get; set; }
    }
}
