using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.DataTransfer.Services
{
    public interface IDataSourceService : IBaseService
    {
        Task<T> GetDataSourceByNameAsync<T>(string name, string storageAccount);
    }
}
