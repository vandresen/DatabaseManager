namespace DatabaseManager.Services.IndexSqlite.Services
{
    public interface IIndexAccess
    {
        Task BuildIndex(BuildIndexParameters idxParms);
        Task CreateDatabaseIndex();
        Task<IEnumerable<DmsIndex>> GetDmIndexes(int indexId, string project);
        Task DeleteIndexes(string connectionString, string project);
        Task DeleteIndex(int id, string connectionString);
        Task<IndexModel> GetIndexFromSP(int id, string connectionString);
        Task<IndexModel> GetIndex(int id, string project);
        Task<IndexModel> GetIndexRoot(string connectionString);
        Task UpdateIndex(IndexModel indexModel, string connectionString);
        Task UpdateIndexes(List<IndexModel> indexes, string connectionString);
        Task InsertSingleIndex(IndexModel indexModel, int parentid, string connectionString, string project);
        Task InsertIndexes(List<IndexModel> indexModel, int parentid, string connectionString, string project);
        void ClearAllQCFlags(string connectionString);
        Task<IEnumerable<IndexModel>> GetDescendants(int id, string project);
        Task<IEnumerable<DmsIndex>> GetNumberOfDescendantsSP(int id, string connectionString);
        Task<IEnumerable<DmsIndex>> GetNumberOfDescendantsByIdAndLevel(string indexNode, int level, string connectionString);
        Task<IEnumerable<IndexModel>> GetIndexes(string project);
        Task<IEnumerable<IndexModel>> GetIndexesWithQcStringFromSP(string qcString, string connectionString);
        Task<IEnumerable<IndexModel>> GetChildrenWithName(string connectionString, string indexNode, string name);
        Task<IEnumerable<EntiretyListModel>> GetEntiretyIndexes(string project, string dataType, string entiretyName, string parentType);
        Task<IEnumerable<IndexModel>> GetNeighbors(int id, string project);
        Task<IEnumerable<IndexModel>> QueriedIndexes(string project, string dataType, string qcString);
        Task<int> GetCount(string connectionString, string query);
        Task<List<string>> GetProjects();
        Task CreateProject(string project);
        Task DeleteProject(string project);
        DataAccessDef GetDataAccessDefinition();
        string GetSelectSQL();
    }
}
