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
        private string connectionString;

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

        public async Task<string> SaveFileUri(string fileShare, string fileName, string fileContent)
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
            return file.Uri.ToString();
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

        public async Task<List<string>> ListFiles(string fileShare)
        {
            List<string> files = new List<string>();
            CloudStorageAccount account = CloudStorageAccount.Parse(connectionString);
            CloudFileClient fileClient = account.CreateCloudFileClient();
            CloudFileShare share = fileClient.GetShareReference(fileShare);
            if (!share.Exists())
            {
                Exception error = new Exception($"Fileshare {fileShare} does not exist ");
                throw error;
            }
            IEnumerable<IListFileItem> fileList = share.GetRootDirectoryReference().ListFilesAndDirectories();
            foreach (IListFileItem listItem in fileList)
            {
                if (listItem.GetType() == typeof(CloudFile))
                {
                    files.Add(listItem.Uri.Segments.Last());
                }
            }
            return files;
        }
    }
}
