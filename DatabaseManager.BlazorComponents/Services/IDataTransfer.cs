using DatabaseManager.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.BlazorComponents.Services
{
    public interface IDataTransfer
    {
        Task Copy(TransferParameters transferParameters);
        Task CopyRemote(TransferParameters transferParameters);
        Task DeleteTable(string source, string table);
        Task<List<TransferParameters>> GetDataObjects(string source);
        Task<List<MessageQueueInfo>> GetQueueMessage();
    }
}
