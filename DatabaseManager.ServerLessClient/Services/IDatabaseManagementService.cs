using DatabaseManager.ServerLessClient.Models;

namespace DatabaseManager.ServerLessClient.Services
{
    public interface IDatabaseManagementService
    {
        Task<T> GetDataAccessDef<T>();
        Task Create(DataModelParameters modelParameters);
        Task CreateSqlite();
    }
}
