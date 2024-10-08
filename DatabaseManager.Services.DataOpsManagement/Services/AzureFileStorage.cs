﻿using Azure.Storage.Files.Shares;
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

        public async Task DeleteFile(string fileShare, string fileName)
        {
            ShareClient share = new ShareClient(_connectionString, fileShare);
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

        public async Task<string> ReadFile(string fileShare, string fileName)
        {
            string json = "";
            ShareClient share = new ShareClient(_connectionString, fileShare);
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

        public async Task SaveFile(string fileShare, string fileName, string fileContent)
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
