using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.File;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DatabaseManager.Server.Services
{
    public class AzureFileStorageService: IFileStorageService
    {
        private readonly string connectionString;

        public AzureFileStorageService(IConfiguration configuration)
        {
            connectionString = configuration.GetConnectionString("AzureStorageConnection");
        }

        public async Task SaveFile(string fileShare, string fileName, string fileContent)
        {
            CloudStorageAccount account = CloudStorageAccount.Parse(connectionString);
            CloudFileClient fileClient = account.CreateCloudFileClient();
            CloudFileShare share = fileClient.GetShareReference(fileShare);
            if (!share.Exists())
            {
                share.Create();
            }
            CloudFileDirectory rootDir = share.GetRootDirectoryReference();
            CloudFile file = rootDir.GetFileReference(fileName);
            if (!file.Exists())
            {
                file.Create(fileContent.Length);
            }

            await file.UploadTextAsync(fileContent);
        }

        public async Task<string> ReadFile(string fileShare, string fileName)
        {
            string json = "";
            CloudStorageAccount account = CloudStorageAccount.Parse(connectionString);
            CloudFileClient fileClient = account.CreateCloudFileClient();
            CloudFileShare share = fileClient.GetShareReference(fileShare);
            if (!share.Exists())
            {
                Exception error = new Exception($"Fileshare {fileShare} does not exist ");
                throw error;
            }
            CloudFileDirectory rootDir = share.GetRootDirectoryReference();
            CloudFile file = rootDir.GetFileReference(fileName);
            if (file.Exists())
            {
                json = await file.DownloadTextAsync();
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
