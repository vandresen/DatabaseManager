using AutoMapper;
using Azure;
using Azure.Data.Tables;
using DatabaseManager.Common.Data;
using DatabaseManager.Common.DBAccess;
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
        private readonly IAzureDataAccess _azureDataTables;
        private readonly ISourceData _sourceData;
        private IMapper _mapper;
        private readonly string container = "sources";

        public Sources(string azureConnectionString)
        {
            var builder = new ConfigurationBuilder();
            IConfiguration configuration = builder.Build();
            _fileStorage = new AzureFileStorageServiceCommon(configuration);
            _fileStorage.SetConnectionString(azureConnectionString);
            _azureDataTables = new AzureDataTable(configuration);
            _azureDataTables.SetConnectionString(azureConnectionString);
            _sourceData = new SourceData(_azureDataTables);
        }

        public async Task<ConnectParameters> GetSourceParameters(string name)
        {
            if (String.IsNullOrEmpty(name))
            {
                Exception error = new Exception($"Sources: Source name is not set");
                throw error;
            }
            var connector = _sourceData.GetSource(name);
            if (connector.SourceType == "DataBase")
            {
                string jsonConnectDef = await _fileStorage.ReadFile("connectdefinition", "PPDMDataAccess.json");
                connector.DataAccessDefinition = jsonConnectDef;
            }

            return connector;
        }

        public async Task<List<ConnectParameters>> GetSources()
        {
            List<ConnectParameters> connectors = _sourceData.GetSources();
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
            _sourceData.SaveSource(connector);
        }

        public async Task UpdateSource(ConnectParameters connector)
        {
            if (connector == null)
            {
                Exception error = new Exception($"Sources: Could not find connector data");
                throw error;
            }
            _sourceData.UpdateSource(connector);
        }

        public async Task DeleteSource(string name)
        {
            if (name == null)
            {
                Exception error = new Exception($"Sources: Must provide a source name");
                throw error;
            }
            _sourceData.DeleteSource(name);
        }
    }
}
