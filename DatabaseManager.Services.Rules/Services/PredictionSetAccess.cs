using Azure;
using Azure.Data.Tables;
using DatabaseManager.Services.Rules.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.Rules.Services
{
    public class PredictionSetAccess : IPredictionSetAccess
    {
        private readonly ITableStorageAccess _az;
        private readonly IFileStorageAccess _af;
        private readonly string container = "predictions";
        private readonly string ruleShare = "rules";
        private readonly string partitionKey = "PPDM";

        public PredictionSetAccess(ITableStorageAccess az, IFileStorageAccess af)
        {
            _az = az;
            _af = af;
        }

        public void DeletePredictionDataSet(string name, string connectionsString)
        {
            _az.SetConnectionString(connectionsString);
            _az.Delete(container, partitionKey, name);
        }

        public PredictionSet GetPredictionDataSet(string name, string connectionsString)
        {
            _az.SetConnectionString(connectionsString);
            TableEntity entity = _az.GetRecord(container, partitionKey, name);
            PredictionSet predictionSets = MapTableEntityToPredictionSetModel(entity);
            return predictionSets;
        }

        public List<PredictionSet> GetPredictionDataSets(string connectionsString)
        {
            _az.SetConnectionString(connectionsString);
            List<PredictionSet> predictionSets = new List<PredictionSet>();
            Pageable<TableEntity> entities = _az.GetRecords(container);
            foreach (TableEntity entity in entities)
            {
                PredictionSet predictionSet = MapTableEntityToPredictionSetModel(entity);
                predictionSets.Add(predictionSet);
            }
            return predictionSets;
        }

        public async Task SavePredictionDataSet(PredictionSet predictionSet, string connectionsString)
        {
            _af.SetConnectionString(connectionsString);
            List<RuleModel> rules = predictionSet.RuleSet;
            string fileName = predictionSet.Name + ".json";
            string content = JsonConvert.SerializeObject(rules, Formatting.Indented);
            string url = await _af.SaveFileUriAsync(ruleShare, fileName, content);
            predictionSet.RuleUrl = url;
            TableEntity entity = MapPredictionSetModelToTableEntity(predictionSet);
            _az.SaveRecord(container, entity);
        }

        public void UpdatePredictionDataSet(PredictionSet predictionSet, string connectionsString)
        {
            TableEntity entity = MapPredictionSetModelToTableEntity(predictionSet);
            _az.UpdateRecord(container, entity);
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
