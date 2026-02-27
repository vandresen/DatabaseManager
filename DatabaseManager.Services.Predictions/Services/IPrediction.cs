using DatabaseManager.Services.Predictions.Models;

namespace DatabaseManager.Services.Predictions.Services
{
    public interface IPrediction
    {
        Task<List<int>> ExecutePredictionAsync(List<IndexDto> indexes, RuleModelDto rule, PredictionParameters parms);
    }
}
