﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DatabaseManager.Server.Services
{
    public interface IFileStorageService
    {
        Task DeleteFile(string fileShare, string fileName);
        Task<List<string>> ListFiles(string fileShare);
        Task<string> ReadFile(string fileShare, string fileName);
        Task SaveFile(string fileShare, string fileName, string fileContent);
        Task<string> SaveFileUri(string fileShare, string fileName, string fileContent);
        void SetConnectionString(string connection);
    }
}
