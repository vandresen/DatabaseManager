using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DatabaseManager.Client.Helpers
{
    public interface IDocumentation
    {
        Task<string> GetDoc(string docName);
    }
}
