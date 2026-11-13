using DatabaseManager.Services.RulesSqlite.Models;

namespace DatabaseManager.Services.RulesSqlite.Extensions
{
    public static class MappingExtensions
    {
        // RuleFunctions <-> RuleFunctionsDto
        public static RuleFunctions ToModel(this RuleFunctionsDto dto) => dto == null ? null : new RuleFunctions
        {
            Id = dto.Id,
            FunctionName = dto.FunctionName,
            FunctionUrl = dto.FunctionUrl,
            FunctionKey = dto.FunctionKey,
            FunctionType = dto.FunctionType,
        };

        public static RuleFunctionsDto ToDto(this RuleFunctions model) => model == null ? null : new RuleFunctionsDto
        {
            Id = model.Id,
            FunctionName = model.FunctionName,
            FunctionUrl = model.FunctionUrl,
            FunctionKey = model.FunctionKey,
            FunctionType = model.FunctionType,
        };

        // RuleModel <-> RuleModelDto
        public static RuleModel ToModel(this RuleModelDto dto) => dto == null ? null : new RuleModel
        {
            Id = dto.Id,
            Active = dto.Active,
            DataType = dto.DataType,
            DataAttribute = dto.DataAttribute,
            RuleType = dto.RuleType,
            RuleName = dto.RuleName,
            RuleDescription = dto.RuleDescription,
            RuleFunction = dto.RuleFunction,
            RuleKey = dto.RuleKey,
            KeyNumber = dto.KeyNumber,
            RuleParameters = dto.RuleParameters,
            RuleFilter = dto.RuleFilter,
            FailRule = dto.FailRule,
            PredictionOrder = dto.PredictionOrder,
            CreatedBy = dto.CreatedBy,
            CreatedDate = dto.CreatedDate,
            ModifiedBy = dto.ModifiedBy,
            ModifiedDate = dto.ModifiedDate,
        };

        public static RuleModelDto ToDto(this RuleModel model) => model == null ? null : new RuleModelDto
        {
            Id = model.Id,
            Active = model.Active,
            DataType = model.DataType,
            DataAttribute = model.DataAttribute,
            RuleType = model.RuleType,
            RuleName = model.RuleName,
            RuleDescription = model.RuleDescription,
            RuleFunction = model.RuleFunction,
            RuleKey = model.RuleKey,
            KeyNumber = model.KeyNumber,
            RuleParameters = model.RuleParameters,
            RuleFilter = model.RuleFilter,
            FailRule = model.FailRule,
            PredictionOrder = model.PredictionOrder,
            CreatedBy = model.CreatedBy,
            CreatedDate = model.CreatedDate,
            ModifiedBy = model.ModifiedBy,
            ModifiedDate = model.ModifiedDate,
        };

        // Lists
        public static List<RuleModel> ToModels(this IEnumerable<RuleModelDto> dtos) => dtos?.Select(d => d.ToModel()).ToList();
        public static List<RuleModelDto> ToDtos(this IEnumerable<RuleModel> models) => models?.Select(m => m.ToDto()).ToList();
        public static List<RuleFunctions> ToModels(this IEnumerable<RuleFunctionsDto> dtos) => dtos?.Select(d => d.ToModel()).ToList();
        public static List<RuleFunctionsDto> ToDtos(this IEnumerable<RuleFunctions> models) => models?.Select(m => m.ToDto()).ToList();
    }
}
