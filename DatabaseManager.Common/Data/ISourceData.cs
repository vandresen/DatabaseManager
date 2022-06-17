using DatabaseManager.Shared;
using System.Collections.Generic;

namespace DatabaseManager.Common.Data
{
    public interface ISourceData
    {
        List<ConnectParameters> GetSources();
        ConnectParameters GetSource(string name);
        void SaveSource(ConnectParameters connector);
        void UpdateSource(ConnectParameters connector);
        void DeleteSource(string name);
    }
}
