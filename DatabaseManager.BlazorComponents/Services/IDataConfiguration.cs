using DatabaseManager.BlazorComponents.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.BlazorComponents.Services
{
    public interface IDataConfiguration
    {
        Task<ResponseDto> GetRecords();
        Task<ResponseDto> GetRecord(string name);
    }
}
