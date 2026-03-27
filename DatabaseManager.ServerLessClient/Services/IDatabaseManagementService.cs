namespace DatabaseManager.ServerLessClient.Services
{
    public interface IDatabaseManagementService
    {
        Task<T> GetDataAccessDef<T>();
    }
}
