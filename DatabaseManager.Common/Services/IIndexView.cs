using DatabaseManager.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Common.Services
{
    public interface IIndexView
    {
        void InitSettings(SingletonService settings);
        Task<List<DmsIndex>> GetChildren(string source, int id);
        Task<List<DmsIndex>> GetIndex(string source);
    }
}
