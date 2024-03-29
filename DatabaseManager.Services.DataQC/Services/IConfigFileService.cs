﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.DataQC.Services
{
    public interface IConfigFileService
    {
        Task<T> GetConfigurationFileAsync<T>(string name);
    }
}
