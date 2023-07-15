using DatabaseManager.Services.Index.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.Index.Services
{
    public interface IIndexDBAccess
    {
        string GetSelectSQL();
        Task BuildIndex(BuildIndexParameters idxParms);
        Task CreateDatabaseIndex(string connectionString);
        Task<IEnumerable<IndexDto>> GetIndexes(string connectionString);
        Task<IndexDto> GetIndex(int id, string connectionString);
        Task<IEnumerable<DmIndexDto>> GetDmIndexes(string indexNode, int level, string connectionString);
        Task<IEnumerable<DmIndexDto>> GetDmIndex(int id, string connectionString);
    }
}
