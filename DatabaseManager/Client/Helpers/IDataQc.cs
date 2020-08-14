using DatabaseManager.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DatabaseManager.Client.Helpers
{
    public interface IDataQc
    {
        Task<List<DmsIndex>> GetQcFailures(string source, int id);
        Task<List<QcResult>> GetQcResult(string source);
        Task ProcessQCRule(DataQCParameters qcParams);
    }
}
