namespace DatabaseManager.ServerLessClient.Services
{
    public interface IDataConfigurationService : IBaseService
    {
        Task<T> GetRecords<T>();
        Task<T> GetRecord<T>(string name);
        Task<T> SaveRecords<T>(string name, object body);
        Task<T> DeleteRecord<T>(string name);
    }
}
