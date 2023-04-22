using DatabaseManager.Services.DataTransfer.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.DataTransfer.Services
{
    public class LASTransfer : IDataTransfer
    {
        private readonly IFileStorageService _fileStorage;

        public LASTransfer()
        {
            _fileStorage = new AzureFileStorageService();
        }
        public void CopyData(TransferParameters transferParameters, ConnectParametersDto sourceConnector, ConnectParametersDto targetConnector, string referenceJson)
        {
            throw new NotImplementedException();
        }

        public void DeleteData(ConnectParametersDto source, string table)
        {
            throw new NotImplementedException();
        }

        public async Task<List<string>> GetContainers(ConnectParametersDto source)
        {
            List<string> files = new List<string>();
            _fileStorage.SetConnectionString(source.ConnectionString);
            files = await _fileStorage.ListFiles(source.Catalog);
            return files;
        }
    }
}
