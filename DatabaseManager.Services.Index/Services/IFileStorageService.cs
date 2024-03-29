﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.Index.Services
{
    public interface IFileStorageService
    {
        Task<string> ReadFile(string fileShare, string fileName);
        Task<List<string>> ListFiles(string fileShare);
        void SetConnectionString(string connection);
    }
}
