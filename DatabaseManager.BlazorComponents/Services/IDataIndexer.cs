using DatabaseManager.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.BlazorComponents.Services
{
    public interface IDataIndexer
    {
        Task Create(CreateIndexParameters iParameters);
        Task<List<IndexFileList>> GetTaxonomies();
        Task<CreateIndexParameters> GetTaxonomy(string Name);
    }
}
