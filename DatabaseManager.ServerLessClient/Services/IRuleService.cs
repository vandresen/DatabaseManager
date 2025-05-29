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
        //Task<T> DeleteFunctionAsync<T>(string source, int id);
        //Task<T> InsertFunctionAsync<T>(RuleFunctions function, string source);
        //Task<T> UpdateFunctionAsync<T>(RuleFunctions function, string source, int id);
        //Task<T> GetPredictionsAsync<T>();
        //Task<T> InsertPredictionAsync<T>(PredictionSet predictionSet, string predictionName);
        //Task<T> GetPredictionAsync<T>(string predictionName);
    }
}
