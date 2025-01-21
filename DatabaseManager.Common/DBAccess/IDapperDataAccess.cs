using System.Collections.Generic;
using System.Threading.Tasks;

namespace DatabaseManager.Common.DBAccess
{
    public interface IDapperDataAccess
    {
        Task<IEnumerable<T>> LoadData<T, U>(string storedProcedure, U parameters, string connectionString);
        Task<IEnumerable<T>> ReadData<T>(string sql, string connectionString);
        Task SaveData<T>(string storedProcedure, T parameters, string connectionString);
        Task SaveDataSQL<T>(string sql, T parameters, string connectionString);
        Task<T> SaveDataScalar<T, U>(string storedProcedure, U parameters, string connectionString);
        Task<T> Count<T, U>(string sql, U parameters, string connectionString);
        Task DeleteData<T>(string sql, T parameters, string connectionString);
    }
}