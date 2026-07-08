using DatabaseManager.ServerLessClient.Models;

namespace DatabaseManager.ServerLessClient.Extensions
{
    public static class MappingExtensions
    {
        public static RuleFunction ToRuleFunction(this RuleFunctionDto dto) => new()
        {
            Id = dto.Id,
            FunctionName = dto.FunctionName,
            FunctionUrl = dto.FunctionUrl,
            FunctionKey = dto.FunctionKey,
            FunctionType = dto.FunctionType ?? ""
        };
    }
}
