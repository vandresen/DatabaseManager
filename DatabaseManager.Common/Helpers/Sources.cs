using AutoMapper;
using DatabaseManager.Common.Entities;
using DatabaseManager.Common.Services;
using DatabaseManager.Shared;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Common.Helpers
{
    public class Sources
    {
        private readonly IFileStorageServiceCommon _fileStorage;
        private readonly ITableStorageServiceCommon _tableStorage;
        private DbUtilities _dbConn;
        private IMapper _mapper;
        private readonly string container = "sources";

        public Sources(string azureConnectionString)
        {
            var builder = new ConfigurationBuilder();
            IConfiguration configuration = builder.Build();
            _fileStorage = new AzureFileStorageServiceCommon(configuration);
            _fileStorage.SetConnectionString(azureConnectionString);
            _tableStorage = new AzureTableStorageServiceCommon(configuration);
            _tableStorage.SetConnectionString(azureConnectionString);

            _dbConn = new DbUtilities();

            var config = new MapperConfiguration(cfg => {
                cfg.CreateMap<SourceEntity, ConnectParameters>().ForMember(dest => dest.SourceName, opt => opt.MapFrom(src => src.RowKey));
                cfg.CreateMap<ConnectParameters, SourceEntity>().ForMember(dest => dest.RowKey, opt => opt.MapFrom(src => src.SourceName));
            });
            _mapper = config.CreateMapper();
        }

        public async Task<ConnectParameters> GetSourceParameters(string name)
        {
            if (String.IsNullOrEmpty(name))
            {
                Exception error = new Exception($"Sources: Source name is not set");
                throw error;
            }
            ConnectParameters connector = new ConnectParameters();
            SourceEntity entity = await _tableStorage.GetTableRecord<SourceEntity>(container, name);
            if (entity == null)
            {
                Exception error = new Exception($"Sources: Could not find source connector");
                throw error;
            }
            connector = _mapper.Map<ConnectParameters>(entity);
            if (connector.SourceType == "DataBase")
            {
                string jsonConnectDef = await _fileStorage.ReadFile("connectdefinition", "PPDMDataAccess.json");
                connector.DataAccessDefinition = jsonConnectDef;
            }

            return connector;
        }

        public async Task<List<ConnectParameters>> GetSources()
        {
            List<ConnectParameters> connectors = new List<ConnectParameters>();
            List<SourceEntity> sourceEntities = new List<SourceEntity>();
            sourceEntities = await _tableStorage.GetTableRecords<SourceEntity>(container);
            connectors = _mapper.Map<List<ConnectParameters>>(sourceEntities);
            string jsonConnectDef = await _fileStorage.ReadFile("connectdefinition", "PPDMDataAccess.json");
            foreach (var connector in connectors)
            {
                if (connector.SourceType == "DataBase")
                {
                    connector.DataAccessDefinition = jsonConnectDef;
                } 
            }
            return connectors;
        }

        public async Task SaveSource(ConnectParameters connector)
        {
            if (connector == null)
            {
                Exception error = new Exception($"Sources: Could not find connector data");
                throw error;
            }
            SourceEntity sourceEntity = _mapper.Map<SourceEntity>(connector);
            await _tableStorage.SaveTableRecord(container, connector.SourceName, sourceEntity);
        }

        public async Task UpdateSource(ConnectParameters connector)
        {
            if (connector == null)
            {
                Exception error = new Exception($"Sources: Could not find connector data");
                throw error;
            }
            SourceEntity sourceEntity = _mapper.Map<SourceEntity>(connector);
            await _tableStorage.UpdateTable(container, sourceEntity);
        }

        public async Task DeleteSource(string name)
        {
            if (name == null)
            {
                Exception error = new Exception($"Sources: Must provide a source name");
                throw error;
            }
            await _tableStorage.DeleteTable(container, name);
        }
    }
}
