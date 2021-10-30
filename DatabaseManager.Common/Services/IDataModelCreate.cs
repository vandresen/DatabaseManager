using DatabaseManager.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Common.Services
{
    public interface IDataModelCreate
    {
        Task Create(DataModelParameters modelParameters);
    }
}
