using System.ComponentModel.DataAnnotations;

namespace DatabaseManager.ServerLessClient.Models
{
    class ValidateFailRule : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            RuleModel rule = (RuleModel)validationContext.ObjectInstance;
            if (rule.RuleType == "Predictions")
            {
                if (string.IsNullOrEmpty(rule.FailRule))
                {
                    return new ValidationResult("Fail rule is required.", new[] { validationContext.MemberName });
                }
                else
                {
                    return ValidationResult.Success;
                }
            }
            else
            {
                return ValidationResult.Success;
            }
        }
    }
}
