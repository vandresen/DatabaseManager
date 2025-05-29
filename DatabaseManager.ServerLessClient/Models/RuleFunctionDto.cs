using System.ComponentModel.DataAnnotations;

namespace DatabaseManager.ServerLessClient.Models
{
    public class RuleFunctionDto
    {
        public int Id { get; set; }
        public string FunctionName { get; set; }
        public string FunctionUrl { get; set; }
        public string FunctionKey { get; set; }
        public string FunctionType { get; set; }
    }
}
