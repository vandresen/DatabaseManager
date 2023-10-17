using System.ComponentModel.DataAnnotations;

namespace DatabaseManager.Services.RulesSqlite.Models
{
    public class RuleFunctions
    {
        public int Id { get; set; }

        [Required]
        [StringLength(40)]
        public string FunctionName { get; set; }

        [Required]
        [StringLength(255)]
        public string FunctionUrl { get; set; }

        [StringLength(255)]
        public string FunctionKey { get; set; }

        [StringLength(1)]
        public string FunctionType { get; set; }
    }
}
