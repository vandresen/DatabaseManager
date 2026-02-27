using DatabaseManager.Services.Predictions.Models;

namespace DatabaseManager.Services.Predictions.Services
{
    public interface IBaseService
    {
        Task<T> SendAsync<T>(ApiRequest apiRequest);
    }
}
