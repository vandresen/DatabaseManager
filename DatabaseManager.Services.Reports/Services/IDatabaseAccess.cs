using DatabaseManager.Services.Reports.Models;

namespace DatabaseManager.Services.Reports.Services
{
    public interface IDatabaseAccess
    {
        Task<IEnumerable<TableSchema>> GetColumnInfo(string connectionString, string table);
    }
}
