using DatabaseManager.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DatabaseManager.Server.Services
{
    public interface ITableStorageService
    {
        Task DeleteTable(string container, string name);
        Task<ConnectParameters> GetTable(string container, string name);
        Task<List<ConnectParameters>> ListTable(string container, string dataAccessDef);
        Task SaveTable(string container, ConnectParameters connectParameters);
        void SetConnectionString(string connection);
        Task UpdateTable(string container, ConnectParameters connectParameters);
    }
}
