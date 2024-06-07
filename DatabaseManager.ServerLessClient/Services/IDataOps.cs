using DatabaseManager.ServerLessClient.Models;

namespace DatabaseManager.ServerLessClient.Services
{
    public interface IDataOps
    {
        Task CreatePipeline(DataOpsPipes pipe);
        Task DeletePipeline(string name);
        Task<List<PipeLine>> GetPipeline(string name);
        Task<T> GetPipelines<T>();
        Task ProcessPipeline(List<DataOpParameters> parms);
        Task<DataOpsResults> ProcessPipelineWithStatus(List<DataOpParameters> parms);
        Task SavePipeline(DataOpsPipes pipe, List<PipeLine> tubes);
        Task<DataOpsStatus> GetStatus(string url);

    }
}
