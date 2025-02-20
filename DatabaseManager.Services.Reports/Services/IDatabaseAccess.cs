using DatabaseManager.Services.Reports.Models;

namespace DatabaseManager.Services.Reports.Services
{
    public interface IDatabaseAccess
    {
        //Task<IEnumerable<TableSchema>> GetColumnInfo(string connectionString, string table);
        Task<IEnumerable<T>> LoadData<T, U>(string storedProcedure, U parameters, string connectionString);
    }
}
