using DatabaseManager.Services.DataTransfer.Models;

namespace DatabaseManager.Services.DataTransfer.Services
{
    public interface IIndexDataTransferProvider
    {
        Task<IndexModel> GetIndexRoot(string connectionString);
        Task<IEnumerable<IndexModel>> GetIndexesWithDataType(string dataType, string connectionString);
    }
}
