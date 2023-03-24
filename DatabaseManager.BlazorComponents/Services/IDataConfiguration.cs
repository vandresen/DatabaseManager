using DatabaseManager.BlazorComponents.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.BlazorComponents.Services
{
    public interface IDataConfiguration: IBaseService
    {
        Task<T> GetRecords<T>();
        Task<T> GetRecord<T>(string name);
        Task<T> SaveRecords<T>(string name, object body);
        Task<T> DeleteRecord<T>(string name);
    }
}
