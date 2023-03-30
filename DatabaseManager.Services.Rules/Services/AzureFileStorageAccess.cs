using Azure.Storage.Files.Shares.Models;
using Azure.Storage.Files.Shares;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace DatabaseManager.Services.Rules.Services
{
    public class AzureFileStorageAccess : IFileStorageAccess
    {
        private string _connectionString;

        public AzureFileStorageAccess(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("AzureStorageConnection");
        }

        public async Task<string> SaveFileUriAsync(string fileShare, string fileName, string fileContent)
        {
            ShareClient share = new ShareClient(_connectionString, fileShare);
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
            if (!string.IsNullOrEmpty(connection)) _connectionString = connection;
            if (string.IsNullOrEmpty(_connectionString))
            {
                Exception error = new Exception($"Connection string is not set");
                throw error;
            }
        }
    }
}
