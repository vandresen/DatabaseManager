namespace DatabaseManager.Services.DataQuality.Services
{
    public interface IIndexAccess
    {
        Task<T> GetIndexes<T>(string dataSource, string project, string dataType);
        Task<T> GetEntiretyIndexes<T>(string dataSource, string dataType, string entiretyName, string parentType);
    }
}
