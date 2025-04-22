using DatabaseManager.Common.Entities;
using DatabaseManager.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Common.Data
{
    public interface IIndexDBAccess
    {
        Task<IndexModel> GetIndexFromSP(int id, string connectionString);
        Task<IndexModel> GetIndex(int id, string connectionString);
        Task<IndexModel> GetIndexRoot(string connectionString);
        Task UpdateIndex(IndexModel indexModel, string connectionString);
        Task UpdateIndexes(List<IndexModel> indexes, string connectionString);
        Task<int> InsertSingleIndex(IndexModel indexModel, int parentid, string connectionString);
        void ClearAllQCFlags(string connectionString);
        Task<IEnumerable<IndexModel>> GetDescendantsFromSP(int id, string connectionString);
        Task<IEnumerable<DmsIndex>> GetNumberOfDescendantsSP(int id, string connectionString);
        Task<IEnumerable<DmsIndex>> GetNumberOfDescendantsByIdAndLevel(string indexNode, int level, string connectionString);
        Task<IEnumerable<IndexModel>> GetIndexesFromSP(string connectionString);
        Task<IEnumerable<IndexModel>> GetIndexesWithQcStringFromSP(string qcString, string connectionString);
        Task<IEnumerable<IndexModel>> GetChildrenWithName(string connectionString, string indexNode, string name);
        Task<IEnumerable<IndexModel>> GetIndexesWithDataType(string connectionString, string name);
        Task<IEnumerable<NeighbourIndex>> GetNeighbors(int id, string failRule, string path, string connectionString);
        Task<int> GetCount(string connectionString, string query);
        DataAccessDef GetDataAccessDefinition();
        string GetSelectSQL();
    }
}
