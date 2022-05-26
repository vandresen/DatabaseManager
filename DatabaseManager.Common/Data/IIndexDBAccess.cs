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
        Task UpdateIndex(IndexModel indexModel, string connectionString);
        Task<IEnumerable<IndexModel>> GetDescendantsFromSP(int id, string connectionString);
        Task<IEnumerable<DmsIndex>> GetNumberOfDescendantsSP(int id, string connectionString);
        Task<IEnumerable<IndexModel>> GetIndexesFromSP(string connectionString);
        Task<IEnumerable<IndexModel>> GetIndexesWithQcStringFromSP(string qcString, string connectionString);
        Task<IEnumerable<IndexModel>> GetChildrenWithName(string connectionString, string indexNode, string name);
    }
}
