using Azure;
using Azure.Data.Tables;
using DatabaseManager.Common.DBAccess;
using DatabaseManager.Common.Extensions;
using DatabaseManager.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Common.Data
{
    public class SourceData : ISourceData
    {
        private readonly IAzureDataAccess _az;
        private readonly string container = "sources";
        private readonly string partitionKey = "PPDM";

        public SourceData(IAzureDataAccess az)
        {
            _az = az;
        }

        public List<ConnectParameters> GetSources()
        {
            List<ConnectParameters> connectors = new List<ConnectParameters>();
            Pageable<TableEntity> entities = _az.GetRecords(container);
            foreach(TableEntity entity in entities)
            {
                ConnectParameters connector = MapTableEntityToConnectorModel(entity);
                connectors.Add(connector);
            }
            return connectors;
        }

        public ConnectParameters GetSource(string name)
        {
            TableEntity entity = _az.GetRecord(container, partitionKey, name);
            ConnectParameters connector = MapTableEntityToConnectorModel(entity);
            return connector;
        }

        public void SaveSource(ConnectParameters connector)
        {
            TableEntity entity = MapConnectorModelToTableEntity(connector);
            _az.SaveRecord(container, entity);
        }

        public void UpdateSource(ConnectParameters connector)
        {
            TableEntity entity = MapConnectorModelToTableEntity(connector);
            _az.UpdateRecord(container, entity);
        }

        public void DeleteSource(string name)
        {
            _az.Delete(container, partitionKey, name);
        }

        private ConnectParameters MapTableEntityToConnectorModel(TableEntity entity)
        {
            ConnectParameters connector = new ConnectParameters();
            connector.SourceName = entity.RowKey;
            connector.SourceType = entity["SourceType"].ToString();
            connector.Catalog = entity["Catalog"].ToString();
            connector.ConnectionString = entity["ConnectionString"].ToString();
            if (entity["DatabaseServer"] != null) connector.DatabaseServer = entity["DatabaseServer"].ToString();
            if (entity["User"] != null) connector.User = entity["User"].ToString();
            if (entity["Password"] != null) connector.Password = entity["Password"].ToString();
            if (entity["DataType"] != null) connector.DataType = entity["DataType"].ToString();
            if (entity["FileName"] != null) connector.FileName = entity["FileName"].ToString();
            if (entity["CommandTimeOut"] != null) 
            {
                string strNumber = entity["CommandTimeOut"].ToString();
                int? number = strNumber.GetIntFromString();
                if (number != null) connector.CommandTimeOut = (int)number; 
            }
            return connector;
        }

        private TableEntity MapConnectorModelToTableEntity(ConnectParameters connector)
        {
            var entity = new TableEntity("PPDM", connector.SourceName)
            {
                { "SourceType", connector.SourceType },
                { "Catalog", connector.Catalog },
                { "DatabaseServer", connector.DatabaseServer },
                { "User", connector.User },
                { "Password", connector.Password },
                { "DataType", connector.DataType },
                { "FileName", connector.FileName },
                { "CommandTimeOut", connector.CommandTimeOut },
                { "ConnectionString", connector.ConnectionString }
            };
            return entity;
        }
    }
}
