using AutoMapper;
using DatabaseManager.Common.Entities;
using DatabaseManager.Common.Services;
using DatabaseManager.Shared;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Common.Helpers
{
    public class IndexManagement
    {
        private readonly string azureConnectionString;
        private readonly IFileStorageServiceCommon _fileStorage;
        private readonly ITableStorageServiceCommon _tableStorage;
        private readonly string taxonomyShare = "taxonomy";
        private DbUtilities _dbConn;
        private IMapper _mapper;

        public IndexManagement(string azureConnectionString)
        {
            this.azureConnectionString = azureConnectionString;
            var builder = new ConfigurationBuilder();
            IConfiguration configuration = builder.Build();
            _fileStorage = new AzureFileStorageServiceCommon(configuration);
            _fileStorage.SetConnectionString(azureConnectionString);
            _tableStorage = new AzureTableStorageServiceCommon(configuration);
            _tableStorage.SetConnectionString(azureConnectionString);
            _dbConn = new DbUtilities();

            var config = new MapperConfiguration(cfg => {
                cfg.CreateMap<SourceEntity, ConnectParameters>().ForMember(dest => dest.SourceName, opt => opt.MapFrom(src => src.RowKey));
                cfg.CreateMap<ConnectParameters, SourceEntity>().ForMember(dest => dest.RowKey, opt => opt.MapFrom(src => src.SourceName));
            });
            _mapper = config.CreateMapper();
        }

        public async Task<string> GetTaxonomies()
        {
            string result = "";
            List<string> files = await _fileStorage.ListFiles(taxonomyShare);
            List<IndexFileList> indexParms = new List<IndexFileList>();
            foreach (string file in files)
            {
                indexParms.Add(new IndexFileList()
                {
                    Name = file
                });
            }
            result = JsonConvert.SerializeObject(indexParms, Formatting.Indented);
            return result;
        }

        public async Task<string> GetIndexData(string sourceName)
        {
            string result = "";
            ConnectParameters connector = await Common.GetConnectParameters(azureConnectionString, sourceName);
            if (connector.SourceType == "DataBase")
            {
                string jsonConnectDef = await _fileStorage.ReadFile("connectdefinition", "PPDMDataAccess.json");
                connector.DataAccessDefinition = jsonConnectDef;
            }
            else
            {
                Exception error = new Exception($"RuleManagement: data source must be a Database type");
                throw error;
            }
            _dbConn.OpenConnection(connector);
            string strProcedure = $"EXEC spGetNumberOfDescendants '/', 1";
            string query = "";
            DataTable qc = _dbConn.GetDataTable(strProcedure, query);
            List<DmsIndex> index = ProcessAllChildren(qc);
            result = JsonConvert.SerializeObject(index, Formatting.Indented);
            _dbConn.CloseConnection();
            return result;
        }

        public async Task<string> GetIndexItem(string sourceName, int id)
        {
            string result = "[]";
            ConnectParameters connector = await Common.GetConnectParameters(azureConnectionString, sourceName);
            if (connector.SourceType == "DataBase")
            {
                string jsonConnectDef = await _fileStorage.ReadFile("connectdefinition", "PPDMDataAccess.json");
                connector.DataAccessDefinition = jsonConnectDef;
            }
            else
            {
                Exception error = new Exception($"RuleManagement: data source must be a Database type");
                throw error;
            }
            _dbConn.OpenConnection(connector);
            string query = $" where INDEXID = {id}";
            IndexAccess idxAccess = new IndexAccess();
            List<IndexModel> idxResults = idxAccess.SelectIndexesByQuery(query, connector.ConnectionString);
            if (idxResults.Count == 0)
            {
                Exception error = new Exception($"No index item found");
                throw error;
            }
            string indexNode = idxResults[0].TextIndexNode;
            int indexLevel = idxResults[0].IndexLevel + 1;
            string strProcedure = $"EXEC spGetNumberOfDescendants '{indexNode}', {indexLevel}";
            query = "";
            DataTable idx = _dbConn.GetDataTable(strProcedure, query);
            List<DmsIndex> index = ProcessAllChildren(idx);
            result = JsonConvert.SerializeObject(index, Formatting.Indented);
            _dbConn.CloseConnection();
            return result;
        }

        public async Task CreateIndex(CreateIndexParameters indexParm)
        {
            if (string.IsNullOrEmpty(indexParm.Taxonomy))
            {
                Exception error = new Exception($"Taxonomy not selected");
                throw error;
            }
            Sources sr = new Sources(azureConnectionString);
            ConnectParameters target = await sr.GetSourceParameters(indexParm.TargetName);
            ConnectParameters source = await sr.GetSourceParameters(indexParm.SourceName);
            Indexer index = new Indexer(azureConnectionString);
            int parentNodes = await index.Initialize(target, source, indexParm.Taxonomy);
            List<ParentIndexNodes> nodes = await index.IndexParent(parentNodes);
            for (int j = 0; j < nodes.Count; j++)
            {
                ParentIndexNodes node = nodes[j];
                for (int i = 0; i < node.NodeCount; i++)
                {
                    await index.IndexChildren(j, i, node.ParentNodeId);
                }
            }
            index.CloseIndex();
        }

        private List<DmsIndex> ProcessAllChildren(DataTable idx)
        {
            List<DmsIndex> qcIndex = new List<DmsIndex>();

            foreach (DataRow idxRow in idx.Rows)
            {
                string dataType = idxRow["DATATYPE"].ToString();
                string indexId = idxRow["INDEXID"].ToString();
                string jsonData = idxRow["JSONDATAOBJECT"].ToString();
                int intIndexId = Convert.ToInt32(indexId);
                int nrOfObjects = Convert.ToInt32(idxRow["NumberOfDataObjects"]);
                qcIndex.Add(new DmsIndex()
                {
                    Id = intIndexId,
                    DataType = dataType,
                    NumberOfDataObjects = nrOfObjects,
                    JsonData = jsonData
                });
            }

            return qcIndex;
        }
    }
}
