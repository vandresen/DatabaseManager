using DatabaseManager.Services.Predictions.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.Predictions.Services
{
    public interface IIndexAccess
    {
        Task<T> GetIndexes<T>(string dataSource, string project, string dataType, string storageConnection);
        Task<T> UpdateIndexes<T>(List<IndexDto> indexes, string dataSource, string project, string storageConnection);
        Task<T> InsertIndexes<T>(List<IndexDto> indexes, string dataSource, string project, string storageConnection);
        Task<T> InsertIndex<T>(IndexDto index, string dataSource, string project, string storageConnection);
        Task<T> GetDescendants<T>(int id, string dataSource, string project, string storageConnection);
        Task<T> GetIndex<T>(int id, string project, string storageConnection);
        Task<T> GetRootIndex<T>(string dataSource, string project, string storageConnection);
    }
}
