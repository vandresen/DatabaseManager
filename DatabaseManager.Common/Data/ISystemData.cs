using DatabaseManager.Common.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Common.Data
{
    public interface ISystemData
    {
        Task<string> GetUserName(string connectionString);
        Task<IEnumerable<TableSchema>> GetColumnSchema(string connectionString, string table);

        Task<IEnumerable<TableSchema>> GetColumnInfo(string connectionString, string table);
        Task<IEnumerable<SystemDBData.ForeignKeyInfo>> GetForeignKeyInfo(string connectionString, string table, string column);
    }
}
