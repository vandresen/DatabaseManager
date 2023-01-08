using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.Index.Services
{
    public interface IDapperDataAccess
    {
        Task<IEnumerable<T>> LoadData<T, U>(string storedProcedure, U parameters, string connectionString);
        Task<IEnumerable<T>> ReadData<T>(string sql, string connectionString);
        Task SaveData<T>(string storedProcedure, T parameters, string connectionString);
        Task SaveDataSQL<T>(string sql, T parameters, string connectionString);
        Task<T> Count<T, U>(string sql, U parameters, string connectionString);
    }
}
