using DatabaseManager.Common.Services;
using DatabaseManager.Shared;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Common.Helpers
{
    public class DataOps
    {
        private readonly IFileStorageServiceCommon _fileStorage;

        public DataOps(string azureConnectionString)
        {
            var builder = new ConfigurationBuilder();
            IConfiguration configuration = builder.Build();
            _fileStorage = new AzureFileStorageServiceCommon(configuration);
            _fileStorage.SetConnectionString(azureConnectionString);
        }

        public async Task<List<DataOpsPipes>> GetDataOpsPipes()
        {
            List<DataOpsPipes> pipes = new List<DataOpsPipes>();

            List<string> result = await _fileStorage.ListFiles("dataops");
            foreach (string file in result)
            {
                pipes.Add(new DataOpsPipes { Name = file});
            }

            return pipes;
        }
    }
}
