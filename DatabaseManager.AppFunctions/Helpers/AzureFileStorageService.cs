using Azure.Storage.Files.Shares;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.AppFunctions.Helpers
{
    public class AzureFileStorageService : IFileStorageService
    {
        private string connectionString;

        public AzureFileStorageService(IConfiguration configuration)
        {
            connectionString = configuration.GetConnectionString("AzureStorageConnection");
        }

        public async Task SaveFile(string fileShare, string fileName, string fileContent)
        {
            
        }

        public void SetConnectionString(string connection)
        {
            if (!string.IsNullOrEmpty(connection)) connectionString = connection;
            if (string.IsNullOrEmpty(connectionString))
            {
                Exception error = new Exception($"Connection string is not set");
                throw error;
            }
        }

        public async Task<string> ReadFile(string fileShare, string fileName)
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
                Stream fileStream = await file.OpenReadAsync();
                StreamReader reader = new StreamReader(fileStream);
                json = reader.ReadToEnd();
            }
            else
            {
                Exception error = new Exception($"File {fileName} does not exist in Azure storage ");
                throw error;
            }
            return json;
        }

        public async Task<List<string>> ListFiles(string fileShare)
        {
            List<string> files = new List<string>();
            
            return files;
        }

        public async Task DeleteFile(string fileShare, string fileName)
        {
            List<string> files = new List<string>();
            
        }
    }
}
