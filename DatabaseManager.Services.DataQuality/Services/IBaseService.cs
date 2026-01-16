using DatabaseManager.Services.DataQuality.Models;

namespace DatabaseManager.Services.DataQuality.Services
{
    public interface IBaseService
    {
        Task<T> SendAsync<T>(ApiRequest apiRequest);
    }

}
