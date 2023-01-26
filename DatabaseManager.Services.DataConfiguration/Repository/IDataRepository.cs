using Azure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.DataConfiguration.Repository
{
    public interface IDataRepository
    {
        void SetConnectionString(string connection);
        Task<List<string>> GetRecords(string container);
        Task<string> GetRecord(string container, string file);
        Task SaveRecord(string container, string file, string fileContent);
        void Delete(string container, string file);
    }
}
