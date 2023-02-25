using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.DatabaseManagement.Services
{
    public class AzureFileStorageService : IFileStorageService
    {
        private readonly string _azureStorageAccount;

        public AzureFileStorageService(string azureStorageAccount)
        {
            _azureStorageAccount = azureStorageAccount;
        }
        public async Task<string> ReadFile(string fileShare, string fileName)
        {
            string fileContent = "";
            ShareClient share = new ShareClient(_azureStorageAccount, fileShare);
            if (!share.Exists())
            {
                Exception error = new Exception($"Fileshare {fileName} does not exist ");
                throw error;
            }
            ShareDirectoryClient directory = share.GetRootDirectoryClient();
            ShareFileClient file = directory.GetFileClient(fileName);
            if (file.Exists())
            {
                using (Stream fileStream = await file.OpenReadAsync())
                {
                    using (StreamReader reader = new StreamReader(fileStream))
                    {
                        fileContent = reader.ReadToEnd();
                    }
                }
            }
            else
            {
                Exception error = new Exception($"File {fileName} does not exist in Azure storage ");
                throw error;
            }
            return fileContent;
        }

        public async Task SaveFile(string fileShare, string fileName, string fileContent)
        {
            ShareClient share = new ShareClient(_azureStorageAccount, fileShare);
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
    }
}
