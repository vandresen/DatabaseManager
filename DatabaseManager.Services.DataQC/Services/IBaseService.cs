using DatabaseManager.Services.DataQC.Models;

namespace DatabaseManager.Services.DataQC.Services
{
    public interface IBaseService : IDisposable
    {
        ResponseDto responseModel { get; set; }
        Task<T> SendAsync<T>(ApiRequest apiRequest);
    }
}
