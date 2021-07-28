using DatabaseManager.Common.Services;
using DatabaseManager.Shared;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Common.Helpers
{
    public class DataTransfer
    {
        private readonly IFileStorageServiceCommon _fileStorage;
        private readonly ITableStorageServiceCommon _tableStorage;
        private string _azureConnectionString;

        public DataTransfer(string azureConnectionString)
        {
            var builder = new ConfigurationBuilder();
            IConfiguration configuration = builder.Build();
            _fileStorage = new AzureFileStorageServiceCommon(configuration);
            _fileStorage.SetConnectionString(azureConnectionString);
            _tableStorage = new AzureTableStorageServiceCommon(configuration);
            _tableStorage.SetConnectionString(azureConnectionString);
            _azureConnectionString = azureConnectionString;
        }

        public async Task<List<string>> GetFiles(string source)
        {
            List<string> files = new List<string>();
            try
            {
                ConnectParameters connector = await Common.GetConnectParameters(_azureConnectionString, source);

                if (connector.SourceType == "DataBase")
                {
                    foreach (string tableName in DatabaseTables.Names)
                    {
                        files.Add(tableName);
                    }
                }
                else if (connector.SourceType == "File")
                {
                    if (connector.DataType == "Logs")
                    {
                        files = await _fileStorage.ListFiles(connector.Catalog);
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(connector.FileName))
                        {
                            Exception error = new Exception($"DataTransfer: Could not get filename for {source}");
                            throw error;
                        }
                        files.Add(connector.FileName);
                    }
                }
            }
            catch (Exception ex)
            {
                Exception error = new Exception($"DataTransfer: Could not get files for {source}, {ex}");
                throw error;
            }

            return files;
        }

        public async Task CopyFiles(TransferParameters parms)
        {
            try
            {
                ConnectParameters sourceConnector = await Common.GetConnectParameters(_azureConnectionString, parms.SourceName);
                ConnectParameters targetConnector = await Common.GetConnectParameters(_azureConnectionString, parms.TargetName);
                string dataAccessDefinition = await _fileStorage.ReadFile("connectdefinition", "PPDMDataAccess.json");
                string referenceJson = await _fileStorage.ReadFile("connectdefinition", "PPDMReferenceTables.json");
                targetConnector.DataAccessDefinition = dataAccessDefinition;
                if (sourceConnector.SourceType == "DataBase")
                {
                    DatabaseLoader dl = new DatabaseLoader();
                    dl.CopyTable(parms, sourceConnector.ConnectionString, targetConnector.ConnectionString);
                }
                else if (sourceConnector.SourceType == "File")
                {
                    if (sourceConnector.DataType == "Logs")
                    {
                        LASLoader ls = new LASLoader(_fileStorage);
                        await ls.LoadLASFile(sourceConnector, targetConnector, parms.Table, referenceJson);
                    }
                    else
                    {
                        CSVLoader cl = new CSVLoader(_fileStorage);
                        await cl.LoadCSVFile(sourceConnector, targetConnector, parms.Table);
                    }
                }
                else
                {
                    Exception error = new Exception($"DataTransfer: Not a valid source type for {sourceConnector.SourceName}");
                    throw error;
                }
            }
            catch (Exception ex )
            {
                Exception error = new Exception($"DataTransfer: Problems transfer files/tables, {ex}");
                throw error;
            }
        }
    }
}
