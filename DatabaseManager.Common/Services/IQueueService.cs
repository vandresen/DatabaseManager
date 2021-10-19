using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseManager.Common.Services
{
    public interface IQueueService
    {
        string GetMessage(string queueName);
        void InsertMessage(string queueName, string message);
        void SetConnectionString(string connection);
    }
}
