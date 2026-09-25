using DatabaseManager.Services.DataOps.Models;

namespace DatabaseManager.Services.DataOps.Services
{
    public interface IIndexAccess
    {
        Task<T> GetIndexes<T>(string dataSource, string project, string dataType, DatabaseProvider databaseProvider);
        Task<T> UpdateIndexes<T>(List<IndexDto> indexes, string dataSource, string project, DatabaseProvider databaseProvider);
        Task<T> BuildIndex<T>(BuildIndexParameters idxParms, DatabaseProvider databaseProvider);
    }
}
