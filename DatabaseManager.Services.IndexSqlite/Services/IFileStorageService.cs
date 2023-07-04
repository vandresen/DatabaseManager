namespace DatabaseManager.Services.IndexSqlite.Services
{
    public interface IFileStorageService
    {
        Task<string> ReadFile(string fileShare, string fileName);
        void SetConnectionString(string connection);
        Task<List<string>> ListFiles(string fileShare);
    }
}
