using DatabaseManager.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.BlazorComponents.Services
{
    public interface IRulesService: IBaseService
    {
        Task<T> GetRulesAsync<T>(string source);
        Task<T> GetRuleAsync<T>(string source, int id);
        Task<T> InsertRuleAsync<T>(RuleModel rule, string source);
        Task<T> UpdateRuleAsync<T>(RuleModel rule, string source);
        Task<T> DeleteRuleAsync<T>(string source, int id);
        Task<T> GetFunctionAsync<T>(string source, int id);
        Task<T> GetFunctionsAsync<T>(string source);
        Task<T> DeleteFunctionAsync<T>(string source, int id);
        Task<T> InsertFunctionAsync<T>(RuleFunctions function, string source);
        Task<T> UpdateFunctionAsync<T>(RuleFunctions function, string source, int id);
        Task<T> GetPredictionsAsync<T>();
        Task<T> InsertPredictionAsync<T>(PredictionSet predictionSet, string predictionName);
        Task<T> GetPredictionAsync<T>(string predictionName);
    }
}
