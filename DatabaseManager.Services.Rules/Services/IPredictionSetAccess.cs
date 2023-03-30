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
        List<PredictionSet> GetPredictionDataSets(string connectionsString);
        PredictionSet GetPredictionDataSet(string name, string connectionsString);
        Task SavePredictionDataSet(PredictionSet predictionSet, string connectionsString);
        void UpdatePredictionDataSet(PredictionSet predictionSet, string connectionsString);
        void DeletePredictionDataSet(string name, string connectionsString);
    }
}
