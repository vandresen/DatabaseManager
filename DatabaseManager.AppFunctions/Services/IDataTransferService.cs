using DatabaseManager.AppFunctions.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.AppFunctions.Services
{
    public interface IDataTransferService
    {
        Task<Response> Copy(TransferParameters transferParameters, ApiInfo apiInfo, string azureStorage);
        Task CopyRemote(TransferParameters transferParameters);
        Task<Response> DeleteTable(string url, string azureStorage);
        Task<Response> GetDataObjects(string url, string azureStorage);
        Task<List<MessageQueueInfo>> GetQueueMessage();
    }
}
