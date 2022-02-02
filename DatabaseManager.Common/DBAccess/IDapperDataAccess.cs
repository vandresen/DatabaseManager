using System.Collections.Generic;
using System.Threading.Tasks;

namespace DatabaseManager.Common.DBAccess
{
    public interface IDapperDataAccess
    {
        Task<IEnumerable<T>> LoadData<T, U>(string storedProcedure, U parameters, string connectionString);
        Task SaveData<T>(string storedProcedure, T parameters, string connectionString);
    }
}