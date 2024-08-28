using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.DataOpsManagement.Services
{
    public interface IFileStorage
    {
        Task<List<string>> ListFiles(string fileShare);
        void SetConnectionString(string connection);
        Task DeleteFile(string fileShare, string fileName);
        Task SaveFile(string fileShare, string fileName, string fileContent);
        Task<string> ReadFile(string fileShare, string fileName);
    }
}
