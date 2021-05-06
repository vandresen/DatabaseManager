using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DatabaseManager.Client.Helpers
{
    public interface IDataOps
    {
        Task<List<string>> GetPipelines();
        Task ProcessPipeline(string name);
    }
}
