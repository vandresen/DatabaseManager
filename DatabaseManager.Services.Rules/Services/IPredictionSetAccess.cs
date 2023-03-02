using DatabaseManager.Services.Rules.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.Rules.Services
{
    public interface IPredictionSetAccess
    {
        List<PredictionSet> GetPredictionDataSets();
        PredictionSet GetPredictionDataSet(string name);
        void SavePredictionDataSet(PredictionSet predictionSet);
        void UpdatePredictionDataSet(PredictionSet predictionSet);
        void DeletePredictionDataSet(string name);
    }
}
