using DatabaseManager.Shared;
using System.Threading.Tasks;

namespace DatabaseManager.BlazorComponents.Services
{
    public interface IDataModelService : IBaseService
    {
        Task<T> Create<T>(DataModelParameters modelParameters);
    }
}
