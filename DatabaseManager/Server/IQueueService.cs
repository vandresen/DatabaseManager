using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DatabaseManager.Server
{
    public interface IQueueService
    {
        void InsertMessage(string queueName, string message);
        void SetConnectionString(string connection);
    }
}
