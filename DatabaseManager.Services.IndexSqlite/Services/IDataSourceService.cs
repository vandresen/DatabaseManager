namespace DatabaseManager.Services.IndexSqlite.Services
{
    public interface IDataSourceService : IBaseService
    {
        Task<T> GetDataSourceByNameAsync<T>(string name);
    }
}
