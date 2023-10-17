using DatabaseManager.Services.RulesSqlite.Models;

namespace DatabaseManager.Services.RulesSqlite.Services
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
