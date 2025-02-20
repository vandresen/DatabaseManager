using Azure;
using DatabaseManager.Services.Datasources.Models;
using DatabaseManager.Services.Datasources.Models.Dto;

namespace DatabaseManager.Services.Datasources.Extensions
{
    public static class DomainToFromMapper
    {
        public static ConnectParameters FromConnectParametersDto(this ConnectParametersDto cp)
        {
            return new ConnectParameters
            {
                SourceName = cp.SourceName,
                SourceType = cp.SourceType,
                Catalog = cp.Catalog,
                DatabaseServer = cp.DatabaseServer,
                User = cp.User, 
                Password = cp.Password,
                ConnectionString = cp.ConnectionString,
                DataType = cp.DataType,
                FileName = cp.FileName,
                CommandTimeOut = cp.CommandTimeOut,
                DataAccessDefinition = cp.DataAccessDefinition
            };
        }

        public static ConnectParametersDto FromDataSourceEntity(this DataSourceEntity cp)
        {
            return new ConnectParametersDto
            {
                SourceName = cp.RowKey,
                SourceType = cp.SourceType,
                Catalog = cp.Catalog,
                DatabaseServer = cp.DatabaseServer,
                User = cp.User,
                Password = cp.Password,
                ConnectionString = cp.ConnectionString,
                DataType = cp.DataType,
                FileName = cp.FileName,
                CommandTimeOut = cp.CommandTimeOut
            };
        }

        public static DataSourceEntity ToDataSourceEntity(this ConnectParameters cp)
        {
            return new DataSourceEntity
            {
                Catalog = cp.Catalog,
                ConnectionString = cp.ConnectionString,
                DatabaseServer = cp.DatabaseServer,
                Password = cp.Password,
                SourceType = cp.SourceType,
                User = cp.User,
                DataType = cp.DataType,
                FileName = cp.FileName,
                CommandTimeOut = cp.CommandTimeOut,
                RowKey = cp.SourceName
            };
        }
    }
}
