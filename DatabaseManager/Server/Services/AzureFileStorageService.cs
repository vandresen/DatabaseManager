using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Server.Services
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
            ShareClient share = new ShareClient(connectionString, fileShare);
            if (!share.Exists())
            {
                share.Create();
            }
            ShareDirectoryClient rootDir = share.GetRootDirectoryClient();
            ShareFileClient file = rootDir.GetFileClient(fileName);
            if (!file.Exists())
            {
                file.Create(fileContent.Length);
            }

            byte[] outBuff = Encoding.ASCII.GetBytes(fileContent);
            ShareFileOpenWriteOptions options = new ShareFileOpenWriteOptions { MaxSize = outBuff.Length };
            var stream = await file.OpenWriteAsync(true, 0, options);
            stream.Write(outBuff, 0, outBuff.Length);
            stream.Flush();
        }

        public async Task<string> SaveFileUri(string fileShare, string fileName, string fileContent)
        {
            ShareClient share = new ShareClient(connectionString, fileShare);
            if (!share.Exists())
            {
                share.Create();
            }
            ShareDirectoryClient rootDir = share.GetRootDirectoryClient();
            ShareFileClient file = rootDir.GetFileClient(fileName);
            if (!file.Exists())
            {
                file.Create(fileContent.Length);
            }

            byte[] outBuff = Encoding.ASCII.GetBytes(fileContent);
            ShareFileOpenWriteOptions options = new ShareFileOpenWriteOptions { MaxSize = outBuff.Length };
            var stream = await file.OpenWriteAsync(true, 0, options);
            stream.Write(outBuff, 0, outBuff.Length);
            stream.Flush();
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
            ShareClient share = new ShareClient(connectionString, fileShare);
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

        public async Task DeleteFile(string fileShare, string fileName)
        {
            ShareClient share = new ShareClient(connectionString, fileShare);
            if (!share.Exists())
            {
                Exception error = new Exception($"Fileshare {fileShare} does not exist ");
                throw error;
            }
            ShareDirectoryClient rootDir = share.GetRootDirectoryClient();
            ShareFileClient file = rootDir.GetFileClient(fileName);
            if (file.Exists())
            {
                await file.DeleteAsync();
            }
            else
            {
                Exception error = new Exception($"File {fileName} does not exist in Azure storage ");
                throw error;
            }
        }
    }
}
