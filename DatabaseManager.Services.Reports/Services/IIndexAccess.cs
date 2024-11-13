using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.Reports.Services
{
    public interface IIndexAccess
    {
        Task<T> GetIndexFailures<T>(string dataSource, string project, string dataType, string qcString);
        Task<T> GetRootIndex<T>(string dataSource, string project);
    }
}
