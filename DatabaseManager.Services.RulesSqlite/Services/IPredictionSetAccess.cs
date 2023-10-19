using DatabaseManager.Services.RulesSqlite.Models;

namespace DatabaseManager.Services.RulesSqlite.Services
{
    public interface IPredictionSetAccess
    {
        Task<IEnumerable<PredictionSet>> GetPredictionDataSets();
        Task<PredictionSet> GetPredictionDataSet(string name);
        Task SavePredictionDataSet(PredictionSet predictionSet);
        Task UpdatePredictionDataSet(PredictionSet predictionSet);
        Task DeletePredictionDataSet(int id);
    }
}
