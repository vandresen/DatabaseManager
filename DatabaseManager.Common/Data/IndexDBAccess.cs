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

        public Task<IEnumerable<IndexModel>> GetDescendantsFromSP(int id, string connectionString) =>
            _dp.LoadData<IndexModel, dynamic>("dbo.spGetDescendants", new { id = id }, connectionString);

        public Task<IEnumerable<IndexModel>> GetIndexesFromSP(string connectionString) =>
            _dp.LoadData<IndexModel, dynamic>("dbo.spGetIndex", new { }, connectionString);

        public Task<IEnumerable<IndexModel>> GetIndexesWithQcStringFromSP(string qcString, string connectionString) =>
            _dp.LoadData<IndexModel, dynamic>("dbo.spGetWithQcStringIndex", new { qcstring = qcString}, connectionString);

        public async Task<IndexModel> GetIndexFromSP(int id, string connectionString)
        {
            var results = await _dp.LoadData<IndexModel, dynamic>("dbo.spGetIndexFromId", new { id = id }, connectionString);
            return results.FirstOrDefault();
        }

        public Task<IEnumerable<DmsIndex>> GetNumberOfDescendantsSP(int id, string connectionString) =>
            _dp.LoadData<DmsIndex, dynamic>("dbo.spGetNumberOfDescendantsById", new { id = id }, connectionString);

        public Task UpdateIndex(IndexModel indexModel, string connectionString) =>
            _dp.SaveData("dbo.spUpdateIndex", new {indexModel.IndexId, indexModel.QC_String, indexModel.JsonDataObject}, connectionString);
    }
}
