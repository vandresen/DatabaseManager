using DatabaseManager.Services.DataOps.Models;

namespace DatabaseManager.Services.DataOps.Services
{
    public interface IBaseService : IDisposable
    {
        ResponseDto responseModel { get; set; }
        Task<T> SendAsync<T>(ApiRequest apiRequest);
    }
}
