using System.Reflection;

namespace DatabaseManager.Services.RulesSqlite.Services
{
    public class EmbeddedFileStorage : IFileStorage
    {
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
