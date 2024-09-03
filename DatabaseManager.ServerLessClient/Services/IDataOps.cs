using DatabaseManager.ServerLessClient.Models;

namespace DatabaseManager.ServerLessClient.Services
{
    public interface IDataOps
    {
        Task<T> CreatePipeline<T>(string name);
        Task<T> DeletePipeline<T>(string name);
        Task<T> GetPipeline<T>(string name);
        Task<T> GetPipelines<T>();
        Task<DataOpsResults> ProcessPipeline(List<DataOpParameters> parms);
        Task<T> SavePipeline<T>(DataOpsPipes pipe, List<DatabaseManager.Shared.PipeLine> tubes);
        Task<DataOpsStatus> GetStatus(string url);

    }
}
