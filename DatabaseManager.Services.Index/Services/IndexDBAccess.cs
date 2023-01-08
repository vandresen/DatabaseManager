using DatabaseManager.Services.Index.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.Index.Services
{
    public class IndexDBAccess : IIndexDBAccess
    {
        private readonly IDapperDataAccess _dp;

        public IndexDBAccess(IDapperDataAccess dp)
        {
            _dp = dp;
        }

        public Task<IEnumerable<IndexDto>> GetIndexes(string connectionString) =>
            _dp.LoadData<IndexDto, dynamic>("dbo.spGetIndex", new { }, connectionString);
    }
}
