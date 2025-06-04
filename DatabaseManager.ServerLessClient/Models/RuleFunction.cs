using System.ComponentModel.DataAnnotations;

namespace DatabaseManager.ServerLessClient.Models
{
    public class RuleFunction
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

        [Required(ErrorMessage = "Function type is required.")]
        [StringLength(1)]
        public string FunctionType { get; set; } = "";
    }
}
