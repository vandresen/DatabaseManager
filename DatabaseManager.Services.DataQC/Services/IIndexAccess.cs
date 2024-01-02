using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.DataQC.Services
{
    public interface IIndexAccess
    {
        Task<T> GetIndexes<T>(string dataSource, string project, string dataType);
        Task<T> GetEntiretyIndexes<T>(string dataSource, string dataType, string entiretyName, string parentType);
    }
}
