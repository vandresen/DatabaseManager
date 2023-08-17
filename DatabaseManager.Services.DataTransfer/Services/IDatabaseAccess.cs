using DatabaseManager.Services.DataTransfer.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.DataTransfer.Services
{
    public interface IDatabaseAccess
    {
        Task<string> GetUserName(string connectionString);
        Task<IEnumerable<TableSchema>> GetColumnInfo(string connectionString, string table);
        Task<int> GetCount(string connectionString, string query);
        Task InsertData(string connectionString, string sql);
        Task SaveData<T>(string storedProcedure, T parameters, string connectionString);
        DataTable GetDataTable(string connectionString, string sql);
        void ExecuteSQL(string sql, string connectionString);
        Task InsertDataTableToDatabase(string connectionString, DataTable table, List<ReferenceTable> referenceTables,
            DataAccessDef accessDef);
        Task<IEnumerable<T>> LoadData<T, U>(string storedProcedure, U parameters, string connectionString);
        Task<IEnumerable<T>> ReadData<T>(string sql, string connectionString);
    }
}
