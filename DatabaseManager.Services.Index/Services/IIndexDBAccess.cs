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
        Task DeleteIndexes(string connectionString);
        Task BuildIndex(BuildIndexParameters idxParms);
        Task CreateDatabaseIndex(string connectionString);
        Task<IEnumerable<IndexDto>> GetIndexes(string connectionString);
        Task<IEnumerable<EntiretyListModel>> GetEntiretyIndexes(string sql, string connectionString);
        Task<IEnumerable<IndexDto>> QueriedIndexes(string connectionString, string dataType, string qcString);
        Task<IndexDto> GetIndex(int id, string connectionString);
        Task<IEnumerable<DmIndexDto>> GetDmIndexes(string indexNode, int level, string connectionString);
        Task<IEnumerable<DmIndexDto>> GetDmIndex(int id, string connectionString);
        Task InsertIndexes(IndexDataCollection indexes, int parentid, string connectionString);
        Task UpdateIndexes(List<IndexDto> indexes, string connectionString);
    }
}
