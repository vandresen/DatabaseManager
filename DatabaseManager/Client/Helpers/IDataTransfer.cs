﻿using DatabaseManager.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DatabaseManager.Client.Helpers
{
    public interface IDataTransfer
    {
        Task Copy(TransferParameters transferParameters);
        Task DeleteTable(string source, string table);
        Task<List<string>> GetDataObjects(string source);
    }
}
