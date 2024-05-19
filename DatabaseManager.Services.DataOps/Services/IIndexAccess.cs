using DatabaseManager.Services.DataOps.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.DataOps.Services
{
    public interface IIndexAccess
    {
        Task<T> GetIndexes<T>(string dataSource, string project, string dataType);
        Task<T> UpdateIndexes<T>(List<IndexDto> indexes, string dataSource, string project);
    }
}
