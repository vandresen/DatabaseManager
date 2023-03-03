﻿using DatabaseManager.Services.Rules.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.Rules.Services
{
    public interface IFunctionAccess
    {
        Task<IEnumerable<RuleFunctionsDto>> GetFunctionsFrom(string connectionString);
    }
}
