using DatabaseManager.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DatabaseManager.Client.Helpers
{
    public interface IDataFile
    {
        Task LoadFile(FileParameters fileParameters);
        Task<List<string>> GetFiles(string dataType);
    }
}
