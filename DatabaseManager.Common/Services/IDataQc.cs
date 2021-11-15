using DatabaseManager.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Common.Services
{
    public interface IDataQc
    {
        Task ClearQCFlags(string source);
        Task<List<DmsIndex>> GetQcFailures(string source, int id);
        Task<List<QcResult>> GetQcResult(string source);
        Task<List<QcResult>> GetResults(string source);
        Task<List<DmsIndex>> GetResult(string source, int id);
        Task ProcessQCRule(DataQCParameters qcParams);
    }
}
