using DatabaseManager.Services.Reports.Models;

namespace DatabaseManager.Services.Reports.Services
{
    public class DatabaseAccess : IDatabaseAccess
    {
        public Task<IEnumerable<TableSchema>> GetColumnInfo(string connectionString, string table)
        {
            throw new NotImplementedException();
        }
    }
}
