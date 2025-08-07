using DatabaseManager.ServerLessClient.Models;

namespace DatabaseManager.ServerLessClient.Services
{
    public class IndexViewSqlLite : BaseService, IIndexView
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly string _taxonomyShare = "taxonomy";

        public IndexViewSqlLite(IHttpClientFactory clientFactory) : base(clientFactory)
        {
            _clientFactory = clientFactory;
        }

        public Task<List<DmsIndex>> GetChildren(string source, int id)
        {
            throw new NotImplementedException();
        }

        public Task<List<DmsIndex>> GetIndex(string source)
        {
            throw new NotImplementedException();
        }

        public Task<List<IndexFileDefinition>> GetIndexFileDefs(string fileName)
        {
            throw new NotImplementedException();
        }

        public Task<List<IndexFileData>> GetIndexTaxonomy(string source)
        {
            throw new NotImplementedException();
        }

        public Task<IndexModel> GetSingleIndexItem(string source, int id)
        {
            throw new NotImplementedException();
        }

        public Task<List<IndexFileList>> GetTaxonomies()
        {
            throw new NotImplementedException();
        }

        public Task SaveIndexFileDefs(List<IndexFileDefinition> indexDef, string fileName)
        {
            throw new NotImplementedException();
        }
    }
}
