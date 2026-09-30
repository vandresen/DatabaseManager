using DatabaseManager.Services.DataOps.Models;

namespace DatabaseManager.Services.DataOps.Services
{
    public interface IPredictionService
    {
        Task<T> ProcessPrediction<T>(PredictionParameters predictionParameter);
    }
}
