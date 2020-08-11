using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DatabaseManager.Server.Services
{
    public interface IFileStorageService
    {
        Task<string> ReadFile(string fileShare, string fileName);
        Task SaveFile(string fileShare, string fileName, string fileContent);
        void SetConnectionString(string connection);
    }
}
