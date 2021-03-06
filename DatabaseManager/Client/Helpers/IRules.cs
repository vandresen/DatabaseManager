﻿using DatabaseManager.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DatabaseManager.Client.Helpers
{
    public interface IRules
    {
        Task DeletePrediction(string predictionName);
        Task DeleteRule(string source, int id);
        Task<List<RuleModel>> GetPrediction(string predictionName);
        Task<List<PredictionSet>> GetPredictions();
        Task<RuleModel> GetRule(string source, int id);
        Task<RuleInfo> GetRuleInfo(string source);
        Task<List<RuleModel>> GetRules(string source);
        Task InsertPrediction(PredictionSet predictionSet, string predictionName);
        Task InsertRule(RuleModel rule, string source);
        Task UpdateRule(RuleModel rule, string source, int id);
    }
}
