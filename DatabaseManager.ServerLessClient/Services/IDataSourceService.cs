using DatabaseManager.ServerLessClient.Models;
using System.Threading.Tasks;

namespace DatabaseManager.ServerLessClient.Services
{
    public interface IDataSourceService : IBaseService
    {
        Task<T> GetAllDataSourcesAsync<T>();
        Task<T> GetDataSourceByNameAsync<T>(string name);
        Task<T> CreateDataSourceAsync<T>(ConnectParametersDto connector);
        Task<T> UpdateDataSourceAsync<T>(ConnectParametersDto connector);
        Task<T> DeleteDataSourceAsync<T>(string name);
    }
}
