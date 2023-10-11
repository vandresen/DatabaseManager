using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.RulesSqlite.Models
{
    public class RuleFunctionsDto
    {
        public int Id { get; set; }

        public string FunctionName { get; set; }

        public string FunctionUrl { get; set; }

        public string FunctionKey { get; set; }

        public string FunctionType { get; set; }
    }
}
