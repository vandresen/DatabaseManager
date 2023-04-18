using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;

namespace DatabaseManager.Services.DataTransfer.Services
{
    public class AzureFileStorageService : IFileStorageService
    {
        private string connectionString;

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

        public Task<string> ReadFile(string fileShare, string fileName)
        {
            throw new NotImplementedException();
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
