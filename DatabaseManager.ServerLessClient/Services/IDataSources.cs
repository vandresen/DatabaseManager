using DatabaseManager.ServerLessClient.Models;

namespace DatabaseManager.ServerLessClient.Services
{
    public interface IDataSources
    {
        Task<T> CreateSource<T>(ConnectParameters connector);
        Task<T> DeleteSource<T>(string name);
        Task<T> GetSource<T>(string Name);
        Task<T> GetSources<T>();
        Task<T> UpdateSource<T>(ConnectParameters connector);
    }
}
