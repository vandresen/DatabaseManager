using DatabaseManager.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.BlazorComponents.Services
{
    public interface IDataQc
    {
        Task<List<QcResult>> GetResults(string source);
        Task<List<DmsIndex>> GetResult(string source, int id);
    }
}
