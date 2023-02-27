using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.DatabaseManagement.Services
{
    public interface IDatabaseAccessService
    {
        Task SaveData<T>(string storedProcedure, T parameters, string connectionString);
        void InsertWithUDT<T>(string storedProcedure, string parameterName, T collection, string connectionString);
        Task<IEnumerable<T>> LoadData<T, U>(string storedProcedure, U parameters, string connectionString);
        Task<IEnumerable<T>> ReadData<T>(string sql, string connectionString);
        void ExecuteSQL(string sql, string connectionString);
        DataTable GetDataTable(string sql, string connectionString);
    }
}
