using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Components.Services
{
    public interface IQueueService
    {
        string GetMessage(string queueName);
        void InsertMessage(string queueName, string message);
        void SetConnectionString(string connection);
    }
}
