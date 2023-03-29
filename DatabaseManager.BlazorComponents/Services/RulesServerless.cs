using DatabaseManager.BlazorComponents.Models;
using DatabaseManager.BlazorComponents.Pages.Rules;
using DatabaseManager.Shared;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.BlazorComponents.Services
{
    public class RulesServerless : IRules
    {
        private readonly IRulesService _rs;

        public RulesServerless(IRulesService rs)
        {
            _rs = rs;
        }

        public async Task DeleteFunction(string source, int id)
        {
            ResponseDto response = await _rs.DeleteFunctionAsync<ResponseDto>(source, id);
            if (!response.IsSuccess)
            {
                throw new ApplicationException(String.Join("; ", response.ErrorMessages));
            }
        }

        public Task DeletePrediction(string predictionName)
        {
            throw new NotImplementedException();
        }

        public async Task DeleteRule(string source, int id)
        {
            ResponseDto response = await _rs.DeleteRuleAsync<ResponseDto>(source, id);
            if (!response.IsSuccess)
            {
                throw new ApplicationException(String.Join("; ", response.ErrorMessages));
            }
        }

        public async Task<RuleFunctions> GetFunction(string source, int id)
        {
            ResponseDto response = await _rs.GetFunctionAsync<ResponseDto>(source, id);
            if (response.IsSuccess)
            {
                return JsonConvert.DeserializeObject<RuleFunctions>(Convert.ToString(response.Result));
            }
            else
            {
                throw new ApplicationException(String.Join("; ", response.ErrorMessages));
            }
        }

        public async Task<List<RuleFunctions>> GetFunctions(string source)
        {
            ResponseDto response = await _rs.GetFunctionsAsync<ResponseDto>(source);
            if (response.IsSuccess)
            {
                return JsonConvert.DeserializeObject<List<RuleFunctions>>(Convert.ToString(response.Result));
            }
            else
            {
                throw new ApplicationException(String.Join("; ", response.ErrorMessages));
            }
        }

        public async Task<List<RuleModel>> GetPrediction(string predictionName)
        {
            ResponseDto response = await _rs.GetPredictionAsync<ResponseDto>(predictionName);
            if (response.IsSuccess)
            {
                return JsonConvert.DeserializeObject<List<RuleModel>>(Convert.ToString(response.Result));
            }
            else
            {
                throw new ApplicationException(String.Join("; ", response.ErrorMessages));
            }
        }

        public async Task<List<PredictionSet>> GetPredictions()
        {
            ResponseDto response = await _rs.GetPredictionsAsync<ResponseDto>();
            if (response.IsSuccess)
            {
                return JsonConvert.DeserializeObject<List<PredictionSet>>(Convert.ToString(response.Result));
            }
            else
            {
                throw new ApplicationException(String.Join("; ", response.ErrorMessages));
            }
        }

        public async Task<RuleModel> GetRule(string source, int id)
        {
            ResponseDto response = await _rs.GetRuleAsync<ResponseDto>(source, id);
            if (response.IsSuccess)
            {
                return JsonConvert.DeserializeObject<RuleModel>(Convert.ToString(response.Result));
            }
            else
            {
                throw new ApplicationException(String.Join("; ", response.ErrorMessages));
            }
        }

        public Task<RuleInfo> GetRuleInfo(string source)
        {
            throw new NotImplementedException();
        }

        public async Task<List<RuleModel>> GetRules(string source)
        {
            ResponseDto response = await _rs.GetRulesAsync<ResponseDto>(source);
            if (response.IsSuccess)
            {
                return JsonConvert.DeserializeObject<List<RuleModel>>(Convert.ToString(response.Result));
            }
            else
            {
                throw new ApplicationException(String.Join("; ", response.ErrorMessages));
            }
        }

        public async Task InsertFunction(RuleFunctions function, string source)
        {
            ResponseDto response = await _rs.InsertFunctionAsync<ResponseDto>(function, source);
            if (!response.IsSuccess)
            {
                throw new ApplicationException(String.Join("; ", response.ErrorMessages));
            }
        }

        public async Task InsertPrediction(PredictionSet predictionSet, string predictionName)
        {
            ResponseDto response = await _rs.InsertPredictionAsync<ResponseDto>(predictionSet, predictionName);
            if (!response.IsSuccess)
            {
                throw new ApplicationException(String.Join("; ", response.ErrorMessages));
            }
        }

        public async Task InsertRule(RuleModel rule, string source)
        {
            ResponseDto response = await _rs.InsertRuleAsync<ResponseDto>(rule, source);
            if (!response.IsSuccess)
            {
                throw new ApplicationException(String.Join("; ", response.ErrorMessages));
            }
        }

        public Task UpdateFunction(RuleFunctions function, string source, int id)
        {
            throw new NotImplementedException();
        }

        public async Task UpdateRule(RuleModel rule, string source, int id)
        {
            ResponseDto response = await _rs.UpdateRuleAsync<ResponseDto>(rule, source);
            if (!response.IsSuccess)
            {
                throw new ApplicationException(String.Join("; ", response.ErrorMessages));
            }
        }
    }
}
