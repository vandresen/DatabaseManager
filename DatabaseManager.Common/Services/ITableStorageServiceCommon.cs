using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Common.Services
{
    public interface ITableStorageServiceCommon
    {
        Task DeleteTable(string container, string name);
        Task<T> GetTableRecord<T>(string container, string name) where T : ITableEntity, new();
        Task<List<T>> GetTableRecords<T>(string container) where T : ITableEntity, new();
        Task SaveTableRecord<T>(string container, string name, T data) where T : TableEntity;
        void SetConnectionString(string connection);
        Task UpdateTable<T>(string container, T data) where T : TableEntity;
    }
}
