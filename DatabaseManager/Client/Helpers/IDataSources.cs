using DatabaseManager.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DatabaseManager.Client.Helpers
{
    public interface IDataSources
    {
        Task CreateSource(ConnectParameters connectParameters);
        Task<ConnectParameters> GetSource(string Name);
        Task<List<ConnectParameters>> GetSources();
        Task UpdateSource(ConnectParameters connectParameters);
    }
}
