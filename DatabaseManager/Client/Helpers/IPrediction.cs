﻿using DatabaseManager.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DatabaseManager.Client.Helpers
{
    public interface IPrediction
    {
        Task<List<DmsIndex>> GetPredictedObjects(string source, int id);
        Task<List<PredictionCorrection>> GetPredictions(string source);
        Task ProcessPredictions(PredictionParameters predictionParams);
    }
}
