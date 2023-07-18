using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.Index.Services
{
    public class EmbeddedFileStorageService : IFileStorageService
    {
        public Task<List<string>> ListFiles(string fileShare)
        {
            throw new NotImplementedException();
        }

        public async Task<string> ReadFile(string fileShare, string fileName)
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            string[] names = asm.GetManifestResourceNames();
            string streamName = @"DatabaseManager.Services.Index.DataModel." + fileName;
            using Stream stream = asm.GetManifestResourceStream(streamName);
            using StreamReader reader = new(stream);
            string fileContent = await reader.ReadToEndAsync();
            return fileContent;
        }

        public void SetConnectionString(string connection)
        {
            throw new NotImplementedException();
        }
    }
}
