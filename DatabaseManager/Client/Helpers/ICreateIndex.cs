using DatabaseManager.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DatabaseManager.Client.Helpers
{
    public interface ICreateIndex
    {
        Task CreateChildIndexes(CreateIndexParameters iParams);
        Task<List<ParentIndexNodes>> CreateParentNodes(CreateIndexParameters iParameters);
        Task<List<string>> GetTaxonomies();
    }
}
