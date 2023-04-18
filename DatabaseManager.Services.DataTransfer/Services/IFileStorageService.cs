using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.DataTransfer.Services
{
    public interface IFileStorageService
    {
        Task<List<string>> ListFiles(string fileShare);
        Task<string> ReadFile(string fileShare, string fileName);
        void SetConnectionString(string connection);
    }
}
