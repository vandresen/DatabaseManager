using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace DatabaseManager.Services.RulesSqlite.Services
{
    public class EmbeddedFileStorage : IFileStorage
    {
        public EmbeddedFileStorage()
        {
        }

        public async Task<string> ReadFile(string fileShare, string fileName)
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            string streamName = @"DatabaseManager.Services.RulesSqlite.Database." + fileName;
            using Stream stream = asm.GetManifestResourceStream(streamName);
            using StreamReader reader = new(stream);
            string fileContent = await reader.ReadToEndAsync();
            return fileContent;
        }
    }
}
