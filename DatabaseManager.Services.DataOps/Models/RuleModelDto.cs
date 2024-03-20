using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.DataOps.Models
{
    public class RuleModelDto
    {
        public int Id { get; set; }

        public string Active { get; set; }

        public string DataType { get; set; }

        public string DataAttribute { get; set; }

        public string RuleType { get; set; }

        public string RuleName { get; set; }

        public string RuleDescription { get; set; }

        public string RuleFunction { get; set; }

        public string RuleKey { get; set; }

        public int KeyNumber { get; set; }

        public string RuleParameters { get; set; }

        public string RuleFilter { get; set; }

        public string FailRule { get; set; }

        public int PredictionOrder { get; set; }

        public string CreatedBy { get; set; }

        public DateTime? CreatedDate { get; set; }

        public string ModifiedBy { get; set; }

        public DateTime? ModifiedDate { get; set; }
    }
}
