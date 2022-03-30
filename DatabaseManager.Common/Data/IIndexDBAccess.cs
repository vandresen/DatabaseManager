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
        Task UpdateIndex(IndexModel indexModel, string connectionString);
    }
}
