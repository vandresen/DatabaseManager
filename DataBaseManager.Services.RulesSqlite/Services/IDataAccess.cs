using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.RulesSqlite.Services
{
    public interface IDataAccess
    {
        Task ExecuteSQL(string sql, string connectionString);
        Task<IEnumerable<T>> ReadData<T>(string sql, string connectionString);
        Task InsertUpdateData<T>(string sql, T parameters, string connectionString);
    }
}
