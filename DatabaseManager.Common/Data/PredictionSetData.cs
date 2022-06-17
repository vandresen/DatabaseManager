using Azure;
using Azure.Data.Tables;
using DatabaseManager.Common.DBAccess;
using DatabaseManager.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Common.Data
{
    public class PredictionSetData : IPredictionSetData
    {
        private readonly IAzureDataAccess _az;
        private readonly string container = "predictions";
        private readonly string partitionKey = "PPDM";

        public PredictionSetData(IAzureDataAccess az)
        {
            _az = az;
        }

        public void DeletePredictionDataSet(string name)
        {
            _az.Delete(container, partitionKey, name);
        }

        public PredictionSet GetPredictionDataSet(string name)
        {
            TableEntity entity = _az.GetRecord(container, partitionKey, name);
            PredictionSet predictionSets = MapTableEntityToPredictionSetModel(entity);
            return predictionSets;
        }

        public List<PredictionSet> GetPredictionDataSets()
        {
            List<PredictionSet> predictionSets = new List<PredictionSet>();
            Pageable<TableEntity> entities = _az.GetRecords(container);
            foreach (TableEntity entity in entities)
            {
                PredictionSet predictionSet = MapTableEntityToPredictionSetModel(entity);
                predictionSets.Add(predictionSet);
            }
            return predictionSets;
        }

        public void SavePredictionDataSet(PredictionSet predictionSet)
        {
            TableEntity entity = MapPredictionSetModelToTableEntity(predictionSet);
            _az.SaveRecord(container, entity);
        }

        public void UpdatePredictionDataSet(PredictionSet predictionSet)
        {
            throw new NotImplementedException();
        }

        private PredictionSet MapTableEntityToPredictionSetModel(TableEntity entity)
        {
            PredictionSet predictionSet = new PredictionSet();
            predictionSet.Name = entity.RowKey;
            if (entity["Description"] != null) predictionSet.Description = entity["Description"].ToString();
            if (entity["RuleUrl"] != null) predictionSet.RuleUrl = entity["RuleUrl"].ToString();
            return predictionSet;
        }

        private TableEntity MapPredictionSetModelToTableEntity(PredictionSet predictionSet)
        {
            var entity = new TableEntity("PPDM", predictionSet.Name)
            {
                { "RuleUrl", predictionSet.RuleUrl},
                { "Description", predictionSet.Description }
            };
            return entity;
        }
    }
}
