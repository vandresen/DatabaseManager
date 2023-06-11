﻿using DatabaseManager.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.BlazorComponents.Services
{
    public interface IPrediction
    {
        Task<List<PredictionCorrection>> GetResults(string source);
        Task ProcessPrediction(PredictionParameters predictionParams);
    }
}
