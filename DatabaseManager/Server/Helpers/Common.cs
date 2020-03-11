using DatabaseManager.Server.Entities;
using DatabaseManager.Shared;
using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DatabaseManager.Server.Helpers
{
    public class Common
    {
        public static CloudTable GetTableConnect(string connectionString, string tableName)
        {
            CloudStorageAccount account = CloudStorageAccount.Parse(connectionString);
            CloudTableClient client = account.CreateCloudTableClient();
            CloudTable table = client.GetTableReference(tableName);
            return table;
        }

        public static ConnectParameters GetConnectParameters(string connectionString, string tableName, string name)
        {
            ConnectParameters connector = new ConnectParameters();
            CloudTable table = Common.GetTableConnect(connectionString, tableName);
            TableOperation retrieveOperation = TableOperation.Retrieve<SourceEntity>("PPDM", name);
            TableResult result = table.Execute(retrieveOperation);
            SourceEntity entity = result.Result as SourceEntity;
            if (entity == null)
            {
                connector = null;
            }
            else
            {
                connector.SourceName = name;
                connector.Database = entity.DatabaseName;
                connector.DatabaseServer = entity.DatabaseServer;
                connector.DatabaseUser = entity.User;
                connector.DatabasePassword = entity.Password;
                connector.ConnectionString = entity.ConnectionString;
            }
            return connector;
        }
    }
}
