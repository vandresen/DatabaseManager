using DatabaseManager.ServerLessClient.Models;

namespace DatabaseManager.ServerLessClient.Services
{
    public interface IRuleService : IBaseService
    {
        Task<List<RuleModelDto>> GetRulesAsync();
        Task<RuleModelDto> GetRuleAsync(string source, int id);
        Task InsertRuleAsync(RuleModel rule);
        Task UpdateRuleAsync(RuleModel rule);
        Task DeleteRuleAsync(int id);
        //Task<T> GetFunctionAsync<T>(string source, int id);
        Task<List<RuleFunctionDto>> GetFunctionsAsync();
        Task DeleteFunctionAsync(int id);
        Task InsertFunctionAsync(RuleFunction function);
        //Task<T> UpdateFunctionAsync<T>(RuleFunctions function, string source, int id);
        Task<List<PredictionSet>> GetPredictionsAsync();
        Task InsertPredictionAsync(PredictionSet predictionSet);
        Task<PredictionSet> GetPredictionAsync(string predictionName);
        Task DeletePredictionAsync(int id);
    }
}
