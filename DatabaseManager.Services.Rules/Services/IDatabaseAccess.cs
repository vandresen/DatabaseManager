using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.Rules.Services
{
    public interface IDatabaseAccess
    {
        Task<IEnumerable<T>> LoadData<T, U>(string storedProcedure, U parameters, string connectionString);
        Task<IEnumerable<T>> ReadData<T>(string sql, string connectionString);
        Task SaveData<T>(string storedProcedure, T parameters, string connectionString);
        Task DeleteData<T>(string sql, T parameters, string connectionString);
        void InsertWithUDT<T>(string storedProcedure, string parameterName, T collection, string connectionString);
    }
}
