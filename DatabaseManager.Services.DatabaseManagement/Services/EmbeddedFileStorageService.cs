using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.DatabaseManagement.Services
{
    public class EmbeddedFileStorageService : IFileStorageService
    {
        public async Task<string> ReadFile(string fileShare, string fileName)
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            string streamName = @"DatabaseManager.Services.DatabaseManagement.Datamodel." + fileName;
            using Stream stream = asm.GetManifestResourceStream(streamName);
            using StreamReader reader = new(stream);
            string fileContent = await reader.ReadToEndAsync();
            return fileContent;
        }

        public Task SaveFile(string fileShare, string fileName, string fileContent)
        {
            throw new NotImplementedException();
        }
    }
}
