using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.DataTransfer.Services
{
    public interface IQueueService
    {
        string GetMessage(string queueName);
        Task InsertMessage(string queueName, string message);
        void SetConnectionString(string connection);
    }
}
