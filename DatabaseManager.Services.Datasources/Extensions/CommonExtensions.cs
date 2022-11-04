using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.Datasources.Extensions
{
    public static class CommonExtensions
    {
        public static string GetStorageKey(this HttpRequest req)
        {
            var headers = req.Headers;
            string storageAccount = headers.FirstOrDefault(x => x.Key.ToLower() == "azurestorageconnection").Value;
            if (string.IsNullOrEmpty(storageAccount))
            {
                Exception error = new Exception($"Error getting azure storage key");
                throw error;
            }
            return storageAccount;
        }
    }
}
