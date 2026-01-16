namespace DatabaseManager.Services.DataQuality.Services
{
    public interface IRuleAccess
    {
        Task<T> GetRule<T>(int id, string sourceName);
    }
}
