using DatabaseManager.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DatabaseManager.Client.Helpers
{
    public interface ICreateIndex
    {
        //Task CreateChildIndexes(CreateIndexParameters iParams);
        Task  Create(CreateIndexParameters iParameters);
        Task<List<string>> GetTaxonomies();
        Task<CreateIndexParameters> GetTaxonomy(string Name);
    }
}
