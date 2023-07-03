using DatabaseManager.Services.IndexSqlite.Models;
using System.Threading.Tasks;

namespace DatabaseManager.Services.IndexSqlite.Services
{
    public interface IIndexAccess
    {
        Task BuildIndex(BuildIndexParameters idxParms);
        Task CreateDatabaseIndex();
        Task DeleteIndexes(string connectionString);
        Task DeleteIndex(int id, string connectionString);
        Task<IndexModel> GetIndexFromSP(int id, string connectionString);
        Task<IndexModel> GetIndex(int id);
        Task<IndexModel> GetIndexRoot(string connectionString);
        Task UpdateIndex(IndexModel indexModel, string connectionString);
        Task UpdateIndexes(List<IndexModel> indexes, string connectionString);
        Task InsertSingleIndex(IndexModel indexModel, int parentid, string connectionString);
        Task InsertIndexes(List<IndexModel> indexModel, int parentid, string connectionString);
        void ClearAllQCFlags(string connectionString);
        Task<IEnumerable<IndexModel>> GetDescendants(int id);
        Task<IEnumerable<DmsIndex>> GetNumberOfDescendantsSP(int id, string connectionString);
        Task<IEnumerable<DmsIndex>> GetNumberOfDescendantsByIdAndLevel(string indexNode, int level, string connectionString);
        Task<IEnumerable<IndexModel>> GetIndexes();
        Task<IEnumerable<IndexModel>> GetIndexesWithQcStringFromSP(string qcString, string connectionString);
        Task<IEnumerable<IndexModel>> GetChildrenWithName(string connectionString, string indexNode, string name);
        Task<IEnumerable<IndexModel>> GetNeighbors(int id);
        Task<int> GetCount(string connectionString, string query);
        DataAccessDef GetDataAccessDefinition();
        string GetSelectSQL();
    }
}
