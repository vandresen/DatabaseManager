using DatabaseManager.Services.DataOps.Models;

namespace DatabaseManager.Services.DataOps.Services
{
    public interface IDataTransferAccess
    {
        Task<T> GetDataObjects<T>(string sourceName, string azureStorage);
        Task<T> DeleteTable<T>(string dataSourceName, string table, string azureStorage);
        Task<T> Copy<T>(TransferParameters transferParameters, string azureStorage);
    }
}
