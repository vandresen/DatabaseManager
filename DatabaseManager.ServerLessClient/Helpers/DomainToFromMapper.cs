
using DatabaseManager.ServerLessClient.Models;

namespace DatabaseManager.ServerLessClient.Helpers
{
    public static class DomainToFromMapper
    {
        public static RuleModel FromRuleModelDto(this RuleModelDto rule)
        {
            return new RuleModel
            {
                Id = rule.Id,
                Active = rule.Active,
                DataType = rule.DataType,
                DataAttribute = rule.DataAttribute,
                RuleType = rule.RuleType,
                RuleName = rule.RuleName,
                RuleDescription = rule.RuleDescription,
                RuleFunction = rule.RuleFunction,
                RuleKey = rule.RuleKey,
                KeyNumber = rule.KeyNumber,
                RuleParameters = rule.RuleParameters,
                RuleFilter = rule.RuleFilter,
                FailRule = rule.FailRule,
                PredictionOrder = rule.PredictionOrder,
                CreatedBy = rule.CreatedBy,
                CreatedDate = rule.CreatedDate,
                ModifiedBy = rule.ModifiedBy,
                ModifiedDate = rule.ModifiedDate

            };
        }
    }
}
