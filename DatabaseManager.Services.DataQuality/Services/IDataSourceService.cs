using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.DataQuality.Services
{
    public interface IDataSourceService
    {
        Task<T> GetDataSourceByNameAsync<T>(string name);
    }
}
