﻿using DatabaseManager.Common.DBAccess;
using DatabaseManager.Common.Entities;
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
        private readonly IADODataAccess _db;
        private string getSql = "Select IndexId, IndexNode.ToString() AS TextIndexNode, " +
            "IndexLevel, DataName, DataType, DataKey, QC_String, UniqKey, JsonDataObject, " +
            "Latitude, Longitude " +
            "from pdo_qc_index";

        public IndexDBAccess()
        {

        }

        public IndexDBAccess(IDapperDataAccess dp)
        {
            _dp = dp;
        }

        public IndexDBAccess(IDapperDataAccess dp, IADODataAccess db)
        {
            _dp = dp;
            _db = db;
        }

        public string GetSelectSQL()
        {
            return getSql;
        }

        public DataAccessDef GetDataAccessDefinition()
        {
            DataAccessDef dataAccessDef = new DataAccessDef();
            dataAccessDef.DataType = "Index";
            dataAccessDef.Select = getSql;
            dataAccessDef.Keys = "INDEXID";
            return dataAccessDef;
        }

        public async Task<IEnumerable<IndexModel>> GetChildrenWithName(string connectionString, string indexNode, string name)
        {
            string sql = $"SELECT DATANAME FROM pdo_qc_index WHERE IndexNode.IsDescendantOf('{indexNode}') = 1 and DATANAME = '{name}'";
            IEnumerable<IndexModel> result = await _dp.ReadData<IndexModel>(sql, connectionString);
            return result;
        }

        public Task<IEnumerable<IndexModel>> GetDescendantsFromSP(int id, string connectionString) =>
            _dp.LoadData<IndexModel, dynamic>("dbo.spGetDescendants", new { id = id }, connectionString);

        public async Task<IndexModel> GetIndex(int id, string connectionString)
        {
            var results = await _dp.LoadData<IndexModel, dynamic>("dbo.spGetIndexFromId", new { id = id }, connectionString);
            return results.FirstOrDefault();
        }

        public async Task<IndexModel> GetIndexRoot(string connectionString)
        {
            var results = await _dp.LoadData<IndexModel, dynamic>("dbo.spGetIndexWithINDEXNODE", new { query = '/' }, connectionString);
            return results.FirstOrDefault();
        }

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

        public Task InsertSingleIndex(IndexModel indexModel, string parentid, string connectionString)
        {
            throw new NotImplementedException();
        }

        public async Task UpdateIndexes(List<IndexModel> indexes, string connectionString)
        {
            string sql = $"update pdo_qc_index set QC_STRING = @QC_String where INDEXID = @IndexId";
            await _dp.SaveDataSQL(sql, indexes, connectionString);
        }

        public void ClearAllQCFlags(string connectionString)
        {
            string sql = "EXEC spClearQCFlags ";
            _db.ExecuteSQL(sql, connectionString);
        }

        public async Task<int> GetCount(string connectionString,string key)
        {
            string query = $"%{key};%";
            string sql = "select count(*) from pdo_qc_index where QC_STRING like @query";
            int count = await _dp.Count<int, dynamic>(sql, new { query = query}, connectionString);
            return count;
        }
    }
}
