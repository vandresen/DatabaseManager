using DatabaseManager.Services.Index.Helpers;
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
        private string getSql = "Select IndexId, IndexNode.ToString() AS TextIndexNode, " +
            "IndexLevel, DataName, DataType, DataKey, QC_String, UniqKey, JsonDataObject, " +
            "Latitude, Longitude " +
            "from pdo_qc_index";

        public IndexDBAccess(IDapperDataAccess dp)
        {
            _dp = dp;
        }

        public Task BuildIndex(BuildIndexParameters idxParms)
        {
            throw new NotImplementedException();
        }

        public async Task CreateDatabaseIndex(string connectionString)
        {
            CreateIndexDataModel dm = new CreateIndexDataModel();
            await dm.CreateDMSModel(connectionString);
            await dm.CreateIndexStoredProcedures(connectionString, getSql);
        }

        public Task<IEnumerable<DmIndexDto>> GetDmIndex(int id, string connectionString) =>
            _dp.LoadData<DmIndexDto, dynamic>("dbo.spGetNumberOfDescendantsById",
                new { id = id }, connectionString);

        public Task<IEnumerable<DmIndexDto>> GetDmIndexes(string indexNode, int level, string connectionString) =>
            _dp.LoadData<DmIndexDto, dynamic>("dbo.spGetNumberOfDescendants",
                new { indexnode = indexNode, level = level }, connectionString);

        public async Task<IndexDto> GetIndex(int id, string connectionString)
        {
            var results = await _dp.LoadData<IndexDto, dynamic>("dbo.spGetIndexFromId", new { id = id }, connectionString);
            return results.FirstOrDefault();
        }

        public Task<IEnumerable<IndexDto>> GetIndexes(string connectionString) =>
            _dp.LoadData<IndexDto, dynamic>("dbo.spGetIndex", new { }, connectionString);

        public string GetSelectSQL()
        {
            return getSql;
        }
    }
}
