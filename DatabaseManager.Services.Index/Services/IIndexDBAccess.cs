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
        Task<IEnumerable<IndexDto>> GetIndexes(string connectionString);
        Task<IndexDto> GetIndex(int id, string connectionString);
    }
}
