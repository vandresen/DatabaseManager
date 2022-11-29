using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.DataConfiguration.Repository
{
    public class AzureFileRepository : IDataRepository
    {
        private string _connectionString;

        public AzureFileRepository()
        {

        }

        public void Delete(string container, string file)
        {
            throw new NotImplementedException();
        }

        public async Task<string> GetRecord(string container, string file)
        {
            string json = "";
            ShareClient share = new ShareClient(_connectionString, container);
            if (!share.Exists())
            {
                Exception error = new Exception($"Fileshare {file} does not exist ");
                throw error;
            }
            ShareDirectoryClient directory = share.GetRootDirectoryClient();
            ShareFileClient shareFile = directory.GetFileClient(file);
            if (shareFile.Exists())
            {
                using (Stream fileStream = await shareFile.OpenReadAsync())
                {
                    using (StreamReader reader = new StreamReader(fileStream))
                    {
                        json = reader.ReadToEnd();
                    }
                }
            }
            else
            {
                Exception error = new Exception($"File {file} does not exist in Azure storage ");
                throw error;
            }
            return json;
        }

        public async Task<List<string>> GetRecords(string container)
        {
            List<string> files = new List<string>();
            ShareClient share = new ShareClient(_connectionString, container);
            if (!share.Exists())
            {
                Exception error = new Exception($"Fileshare {container} does not exist ");
                throw error;
            }
            ShareDirectoryClient directory = share.GetRootDirectoryClient();
            foreach (ShareFileItem item in directory.GetFilesAndDirectories())
            {
                files.Add(item.Name);
            }
            return files;
        }

        public Task SaveRecord(string container, string file)
        {
            throw new NotImplementedException();
        }

        public void SetConnectionString(string connection)
        {
            if (!string.IsNullOrEmpty(connection)) _connectionString = connection;
            if (string.IsNullOrEmpty(_connectionString))
            {
                Exception error = new Exception($"Connection string is not set");
                throw error;
            }
        }
    }
}
