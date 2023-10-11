using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.RulesSqlite.Services
{
    public interface IFileStorage
    {
        Task<string> ReadFile(string fileShare, string fileName);
    }
}
