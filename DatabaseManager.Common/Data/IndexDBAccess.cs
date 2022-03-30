using DatabaseManager.Common.DBAccess;
using DatabaseManager.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Common.Data
{
    public class IndexDBAccess : IIndexDBAccess
    {
        private readonly IDapperDataAccess _dp;

        public IndexDBAccess(IDapperDataAccess dp)
        {
            _dp = dp;
        }
        public async Task<IndexModel> GetIndexFromSP(int id, string connectionString)
        {
            var results = await _dp.LoadData<IndexModel, dynamic>("dbo.spGetIndexFromId", new { id = id }, connectionString);
            return results.FirstOrDefault();
        }

        public Task UpdateIndex(IndexModel indexModel, string connectionString) =>
            _dp.SaveData("dbo.spUpdateIndex", new {indexModel.IndexId, indexModel.QC_String, indexModel.JsonDataObject}, connectionString);
    }
}
