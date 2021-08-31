using DatabaseManager.Common.Services;
using DatabaseManager.Shared;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Common.Helpers
{
    public class DataOpsRepository
    {
        private readonly IFileStorageServiceCommon _fileStorage;
        private string fileShare = "dataops";

        public DataOpsRepository(string azureConnectionString)
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
                pipes.Add(new DataOpsPipes { Name = file });
            }

            return pipes;
        }

        public async Task<List<PipeLine>> GetDataOpsPipe(string name)
        {
            List<PipeLine> dataOps = new List<PipeLine>();

            List<string> result = await _fileStorage.ListFiles("dataops");
            string dataOpsFile = await _fileStorage.ReadFile(fileShare, name);
            dataOps = JsonConvert.DeserializeObject<List<PipeLine>>(dataOpsFile);
            return dataOps;
        }

        public async Task SavePipeline(string name, string content)
        {
            await _fileStorage.SaveFile(fileShare, name, content);
        }

        public async Task DeletePipeline(string name)
        {
            await _fileStorage.DeleteFile(fileShare, name);
        }
    }
}
