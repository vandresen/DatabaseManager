namespace DatabaseManager.Services.RulesSqlite.Services
{
    public interface IFileStorage
    {
        Task<string> ReadFile(string fileShare, string fileName);
    }
}
