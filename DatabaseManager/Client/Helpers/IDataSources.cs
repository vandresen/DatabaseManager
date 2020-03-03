﻿using DatabaseManager.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DatabaseManager.Client.Helpers
{
    public interface IDataSources
    {
        Task<List<ConnectParameters>> GetSources();
    }
}