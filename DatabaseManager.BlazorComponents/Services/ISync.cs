﻿using DatabaseManager.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.BlazorComponents.Services
{
    public interface ISync
    {
        Task<T> GetDataObjects<T>(string sourceName);
        Task<T> TransferIndexObjects<T>(object body);
    }
}
