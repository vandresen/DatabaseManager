using Azure.Storage.Files.Shares;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.Index.Services
{
    public class AzureFileStorageService : IFileStorageService
    {
        private string connectionString;


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
                using (Stream fileStream = await file.OpenReadAsync())
                {
                    using (StreamReader reader = new StreamReader(fileStream))
                    {
                        json = reader.ReadToEnd();
                    }
                }
            }
            else
            {
                Exception error = new Exception($"File {fileName} does not exist in Azure storage ");
                throw error;
            }
            return json;
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
    }
}
