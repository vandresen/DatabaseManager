using DatabaseManager.AppFunctions.Entities;
using System.Threading.Tasks;

namespace DatabaseManager.AppFunctions.Services
{
    public interface IDataIndexer
    {
        Task<Response> Create(BuildIndexParameters iParameters, string url);
    }
}
