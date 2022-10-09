using DatabaseManager.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.BlazorComponents.Services
{
    public interface IIndexView
    {
        void InitSettings(SingletonServices settings);
        Task<List<DmsIndex>> GetChildren(string source, int id);
        Task<List<DmsIndex>> GetIndex(string source);
        Task<IndexModel> GetSingleIndexItem(string source, int id);
        Task<List<IndexFileData>> GetIndexTaxonomy(string source);
        Task<List<IndexFileDefinition>> GetIndexFileDefs(string fileName);
        Task SaveIndexFileDefs(List<IndexFileDefinition> indexDef, string fileName);
    }
}
