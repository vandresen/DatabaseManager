﻿using DatabaseManager.Services.DataTransfer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.DataTransfer.Services
{
    public interface IDataTransfer
    {
        Task<List<string>> GetContainers(ConnectParametersDto source);
        Task CopyData(TransferParameters transferParameters, ConnectParametersDto sourceConnector, ConnectParametersDto targetConnector, string referenceJson);
        void DeleteData(ConnectParametersDto source, string table);
    }
}
