using DatabaseManager.Common.Services;
using DatabaseManager.Shared;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Common.Helpers
{
    public class Indexer
    {
        private readonly IFileStorageServiceCommon _fileStorage;
        private readonly string _azureConnectionString;
        private IndexBuilder iBuilder;

        public Indexer(string azureConnectionString)
        {
            var builder = new ConfigurationBuilder();
            IConfiguration configuration = builder.Build();
            _fileStorage = new AzureFileStorageServiceCommon(configuration);
            _fileStorage.SetConnectionString(azureConnectionString);
            iBuilder = new IndexBuilder();
            _azureConnectionString = azureConnectionString;
        }

        public async Task<int> Initialize(ConnectParameters target, ConnectParameters source, string taxonomyFile, string filter)
        {
            string jsonTaxonomy = await _fileStorage.ReadFile("taxonomy", taxonomyFile);
            int parentNodes = 0;
            if (source.SourceType == "DataBase")
            {
                iBuilder = new IndexBuilder(new DBDataAccess());
            }
            else
            {
                source.ConnectionString = _azureConnectionString;
                if (source.DataType == "Logs")
                {
                    iBuilder = new IndexBuilder(new LASDataAccess(_fileStorage));
                }
                else
                {
                    iBuilder = new IndexBuilder(new CSVDataAccess(_fileStorage));
                }
            }

            iBuilder.InitializeIndex(target, source, jsonTaxonomy);
            iBuilder.CreateRoot(source);
            parentNodes = iBuilder.JsonIndexArray.Count;

            return parentNodes;
        }

        public async Task<List<ParentIndexNodes>> IndexParent(int parentNodes, string filter)
        {
            List<ParentIndexNodes> nodes = new List<ParentIndexNodes>();
            int nodeId = 0;
            for (int k = 0; k < parentNodes; k++)
            {
                JToken token = iBuilder.JsonIndexArray[k];
                int parentCount = await iBuilder.GetObjectCount(token, k, filter);
                if (parentCount > 0)
                {
                    nodeId++;
                    string strNodeId = $"/{nodeId}/";
                    iBuilder.CreateParentNodeIndex(strNodeId);
                    nodes.Add(new ParentIndexNodes()
                    {
                        NodeCount = parentCount,
                        ParentNodeId = strNodeId,
                        Name = (string)token["DataName"]
                    });
                }
            }
            return nodes;
        }

        public async Task IndexChildren(int topId, int parentId, string parentNodeId)
        {
            try
            {
                await iBuilder.PopulateIndex(topId, parentId, parentNodeId);
            }
            catch (Exception ex)
            {
                throw new System.Exception($"Error in IndexChildren: {ex.ToString()}");
            }
        }

        public void CloseIndex()
        {
            iBuilder.CloseIndex();
        }
    }
}
