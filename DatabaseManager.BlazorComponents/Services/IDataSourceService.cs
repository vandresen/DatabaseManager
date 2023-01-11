using DatabaseManager.BlazorComponents.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.BlazorComponents.Services
{
    public interface IDataSourceService : IBaseService
    {
        Task<T> GetAllDataSourcesAsync<T>();
        Task<T> GetDataSourceByNameAsync<T>(string name);
        Task<T> CreateDataSourceAsync<T>(ConnectParametersDto connector);
        Task<T> UpdateDataSourceAsync<T>(ConnectParametersDto connector);
        Task<T> DeleteDataSourceAsync<T>(string name);
    }
}
