using AutoMapper;
using Azure;
using Azure.Data.Tables;
using Azure.Storage.Files.Shares;
using DatabaseManager.Services.Datasources.Models;
using DatabaseManager.Services.Datasources.Models.Dto;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace DatabaseManager.Services.Datasources.Repository
{
    public class DataSourceRepository : IDataSourceRepository
    {
        private const string TableName = "sources";
        private const string PartitionKey = "PPDM";
        private const string fileShare = "connectdefinition";
        private const string fileName = "PPDMDataAccess.json";
        private readonly IMapper _mapper;

        public DataSourceRepository(IMapper mapper)
        {
            _mapper = mapper;
        }

        public async Task<ConnectParametersDto> CreateUpdateDataSource(ConnectParametersDto connectParametersDto, string connectionString)
        {
            var tableClient = await GetTableClient(connectionString);
            ConnectParameters connectParameters = _mapper.Map<ConnectParameters>(connectParametersDto);
            DataSourceEntity entity = _mapper.Map<DataSourceEntity>(connectParameters);
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
            ConnectParametersDto connector = _mapper.Map<ConnectParametersDto>(entity);
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
                ConnectParametersDto connector = _mapper.Map<ConnectParametersDto>(entity);
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
