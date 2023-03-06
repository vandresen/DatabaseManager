using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.Rules.Models
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
