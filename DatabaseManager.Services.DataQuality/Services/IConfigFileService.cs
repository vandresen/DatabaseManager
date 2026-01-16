using DatabaseManager.Services.DataQuality.Models;

namespace DatabaseManager.Services.DataQuality.Services
{
    public interface IConfigFileService
    {
        Task<T> GetConfigurationFileAsync<T>(string name);
    }
}
