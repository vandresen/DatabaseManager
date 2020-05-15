using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DatabaseManager.Shared
{
    public class RuleModel
    {
        public int Id { get; set; }

        [StringLength(1)]
        public string Active { get; set; }

        [Required]
        [StringLength(40)]
        public string DataType { get; set; }

        public string DataAttribute { get; set; }

        [Required]
        [StringLength(40)]
        public string RuleType { get; set; }

        [Required]
        [StringLength(40)]
        public string RuleName { get; set; }

        [StringLength(255)]
        public string RuleDescription { get; set; }

        [StringLength(255)]
        public string RuleFunction { get; set; }

        [StringLength(16)]
        public string RuleKey { get; set; }

        [Required]
        public int KeyNumber { get; set; }

        [StringLength(255)]
        public string RuleParameters { get; set; }

        public string RuleFilter { get; set; }

        public string FailRule { get; set; }

        public int PredictionOrder { get; set; }

        public string CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; }

        public string ModifiedBy { get; set; }

        public DateTime? ModifiedDate { get; set; }
    }
}
