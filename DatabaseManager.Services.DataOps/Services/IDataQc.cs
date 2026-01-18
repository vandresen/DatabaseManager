using DatabaseManager.Services.DataOps.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.DataOps.Services
{
    public interface IDataQc
    {
        Task<T> ExecuteDataQc<T>(DataQCParameters qcParms);
        Task<T> CloseDataQc<T>(string source, string project, List<RuleFailures> ruleFailures);
    }
}
