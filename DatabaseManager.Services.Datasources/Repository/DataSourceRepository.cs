﻿using Azure;
using Azure.Data.Tables;
using Azure.Storage.Files.Shares;
using DatabaseManager.Services.Datasources.Extensions;
using DatabaseManager.Services.Datasources.Models;
using DatabaseManager.Services.Datasources.Models.Dto;

namespace DatabaseManager.Services.Datasources.Repository
{
    public class DataSourceRepository : IDataSourceRepository
    {
        private const string TableName = "sources";
        private const string PartitionKey = "PPDM";
        private const string fileShare = "connectdefinition";
        private const string fileName = "PPDMDataAccess.json";
    

        public DataSourceRepository()
        {
        }

        public async Task<ConnectParametersDto> CreateUpdateDataSource(ConnectParametersDto connectParametersDto, string connectionString)
        {
            var tableClient = await GetTableClient(connectionString);
            ConnectParameters connectParameters = connectParametersDto.FromConnectParametersDto();
            DataSourceEntity entity = connectParameters.ToDataSourceEntity();
            entity.PartitionKey = PartitionKey;
            await tableClient.UpsertEntityAsync(entity);
            return connectParametersDto;
        }

        public async Task<bool> DeleteDataSource(string dataSourceName, string connectionString)
        {
            try
            {
                var tableClient = await GetTableClient(connectionString);
                await tableClient.DeleteEntityAsync(PartitionKey, dataSourceName);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<ConnectParametersDto> GetDataSourceByName(string dataSourceName, string connectionString)
        {
            TableClient tableClient = await GetTableClient(connectionString);
            DataSourceEntity entity = await tableClient.GetEntityAsync<DataSourceEntity>(PartitionKey, dataSourceName);
            ConnectParametersDto connector = entity.FromDataSourceEntity();
            if (connector.SourceType == "DataBase")
            {
                string jsonConnectDef = await ReadFile(connectionString);
                connector.DataAccessDefinition = jsonConnectDef;
            }
            return connector;
        }

        public async Task<List<ConnectParametersDto>> GetDataSources(string connectionString)
        {
            List<ConnectParametersDto> result = new List<ConnectParametersDto>();
            var tableClient = await GetTableClient(connectionString);
            Pageable<DataSourceEntity> entities = tableClient.Query<DataSourceEntity>();
            string jsonConnectDef = await ReadFile(connectionString);
            foreach (DataSourceEntity entity in entities)
            {
                ConnectParametersDto connector = entity.FromDataSourceEntity();
                if (connector.SourceType == "DataBase")
                    connector.DataAccessDefinition = jsonConnectDef;
                result.Add(connector);
            }
            return result;
        }

        private async Task<TableClient> GetTableClient(string connectionString)
        {
            var serviceClient = new TableServiceClient(connectionString);
            var tableClient = serviceClient.GetTableClient(TableName);
            await tableClient.CreateIfNotExistsAsync();
            return tableClient;
        }

        private async Task<string> ReadFile(string connectionString)
        {
            string json = "";
            ShareClient share = new ShareClient(connectionString, fileShare);
            if (!share.Exists())
            {
                Exception error = new Exception($"Fileshare {fileName} does not exist ");
                throw error;
            }
            ShareDirectoryClient directory = share.GetRootDirectoryClient();
            ShareFileClient file = directory.GetFileClient(fileName);
            if (file.Exists())
            {
                using (Stream fileStream = await file.OpenReadAsync())
                {
                    using (StreamReader reader = new StreamReader(fileStream))
                    {
                        json = reader.ReadToEnd();
                    }
                }
            }
            else
            {
                Exception error = new Exception($"File {fileName} does not exist in Azure storage ");
                throw error;
            }
            return json;
        }
    }
}
