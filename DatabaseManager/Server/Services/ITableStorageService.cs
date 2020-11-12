using DatabaseManager.Server.Entities;
using DatabaseManager.Shared;
using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DatabaseManager.Server.Services
{
    public interface ITableStorageService
    {
        Task DeleteTable(string container, string name);
        Task<T> GetTableRecord<T>(string container, string name) where T : ITableEntity, new();
        Task<List<T>> GetTableRecords<T>(string container) where T : ITableEntity, new();
        //Task SaveTable(string container, ConnectParameters connectParameters);
        Task SaveTableRecord<T>(string container, string name, T data) where T : TableEntity;
        void SetConnectionString(string connection);
        Task UpdateTable(string container, ConnectParameters connectParameters);
    }
}
