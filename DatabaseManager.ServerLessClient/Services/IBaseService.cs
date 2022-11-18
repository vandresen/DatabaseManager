using DatabaseManager.ServerLessClient.Models;
using System;
using System.Threading.Tasks;

namespace DatabaseManager.ServerLessClient.Services
{
    public interface IBaseService : IDisposable
    {
        ResponseDto responseModel { get; set; }
        Task<T> SendAsync<T>(ApiRequest apiRequest);
    }
}
