using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.Rules.Services
{
    public interface IFileStorageAccess
    {
        Task<string> SaveFileUriAsync(string fileShare, string fileName, string fileContent);
        void SetConnectionString(string connection);
    }
}
