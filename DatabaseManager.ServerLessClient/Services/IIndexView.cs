using DatabaseManager.ServerLessClient.Models;

namespace DatabaseManager.ServerLessClient.Services
{
    public interface IIndexView
    {
        Task<List<DmsIndex>> GetIndex(string source);
        Task<IndexModel> GetSingleIndexItem(string source, int id);
        Task<List<DmsIndex>> GetChildren(string source, int id);
        //Task<List<IndexFileDefinition>> GetIndexFileDefs(string fileName);
        //Task<List<IndexFileData>> GetIndexTaxonomy(string source);
        Task<List<IndexFileList>> GetTaxonomies();
        Task<List<IndexFileData>> GetIndexTaxonomy(string source);
    }
}
