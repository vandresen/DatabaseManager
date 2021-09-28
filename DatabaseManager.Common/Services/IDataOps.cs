using DatabaseManager.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Common.Services
{
    public interface IDataOps
    {
        Task CreatePipeline(DataOpsPipes pipe);
        Task DeletePipeline(string name);
        Task<List<PipeLine>> GetPipeline(string name);
        Task<List<DataOpsPipes>> GetPipelines();
        Task ProcessPipeline(List<DataOpParameters> parms);
        Task SavePipeline(DataOpsPipes pipe, List<PipeLine> tubes);
    }
}
