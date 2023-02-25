using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.DatabaseManagement.Services
{
    public interface IFileStorageService
    {
        Task<string> ReadFile(string fileShare, string fileName);
        Task SaveFile(string fileShare, string fileName, string fileContent);
    }
}
