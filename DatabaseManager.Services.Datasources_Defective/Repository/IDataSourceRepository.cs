using DatabaseManager.Services.Datasources.Models.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.Datasources.Repository
{
    public interface IDataSourceRepository
    {
        Task<List<ConnectParametersDto>> GetDataSources(string connectionString);
        Task<ConnectParametersDto> GetDataSourceByName(string dataSourceName, string connectionString);
        Task<ConnectParametersDto> CreateUpdateDataSource(ConnectParametersDto connectParameters, string connectionString);
        Task<bool> DeleteDataSource(string dataSourceName, string connectionString);
    }
}
