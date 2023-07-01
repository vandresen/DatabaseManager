using System;
using System.Threading.Tasks;

namespace DatabaseManager.Services.IndexSqlite.Services
{
    public interface IBaseService : IDisposable
    {
        ResponseDto responseModel { get; set; }
        Task<T> SendAsync<T>(ApiRequest apiRequest);
    }
}
