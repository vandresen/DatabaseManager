using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.DataOpsManagement.Services
{
    public class AzureFileStorage : IFileStorage
    {
        private string _connectionString;

        public async Task<List<string>> ListFiles(string fileShare)
        {
            List<string> files = new List<string>();
            ShareClient share = new ShareClient(_connectionString, fileShare);
            if (!share.Exists())
            {
                Exception error = new Exception($"Fileshare {fileShare} does not exist ");
                throw error;
            }
            ShareDirectoryClient directory = share.GetRootDirectoryClient();
            foreach (ShareFileItem item in directory.GetFilesAndDirectories())
            {
                files.Add(item.Name);
            }
            return files;
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
