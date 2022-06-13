using DatabaseManager.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.BlazorComponents.Services
{
    public interface IDataSources
    {
        Task CreateSource(ConnectParameters connectParameters);
        Task DeleteSource(string Name);
        Task<ConnectParameters> GetSource(string Name);
        Task<List<ConnectParameters>> GetSources();
        Task UpdateSource(ConnectParameters connectParameters);
    }
}
