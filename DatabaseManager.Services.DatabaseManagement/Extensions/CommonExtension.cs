using DatabaseManager.Services.DatabaseManagement.Models;
using Microsoft.Azure.Functions.Worker.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.DatabaseManagement.Extensions
{
    public static class CommonExtension
    {
        public static string GetStorageKey(this HttpRequestData req)
        {
            var headers = req.Headers;
            IEnumerable<string> headerSerachResult;
            string storageAccount = string.Empty;
            if (headers.TryGetValues("azurestorageconnection", out headerSerachResult))
            {
                storageAccount = headerSerachResult.First();
            }
            if (string.IsNullOrEmpty(storageAccount))
            {
                Exception error = new Exception($"Error getting azure storage key");
                throw error;
            }
            return storageAccount;
        }

        public static string BuildFunctionUrl(this string url, string function, string query, string apiKey)
        {
            bool buildQuery = false;
            url = url + function;
            if (!string.IsNullOrEmpty(query)) buildQuery = true;
            if (!string.IsNullOrEmpty(apiKey)) buildQuery = true;
            if (buildQuery) url = url + "?";
            if (!string.IsNullOrEmpty(query)) url = url + query + "&";
            if (!string.IsNullOrEmpty(apiKey)) url = url + "code=" + apiKey;
            if (url.EndsWith("&")) url = url.Substring(0, url.Length - 1);
            return url;
        }

        public static string GetDatabaseAttributeType(this TableSchema attributeProperties)
        {
            string attributeType = attributeProperties.TYPE_NAME;
            if (string.IsNullOrEmpty(attributeType)) attributeType = attributeProperties.DATA_TYPE;
            if (attributeType == "nvarchar")
            {
                string charLength = attributeProperties.PRECISION;
                attributeType = attributeType + "(" + charLength + ")";
            }
            if (attributeType == "int identity")
            {
                attributeType = "int";
            }
            else if (attributeType == "numeric")
            {
                string numericPrecision = attributeProperties.PRECISION;
                string numericScale = attributeProperties.SCALE;
                attributeType = attributeType + "(" + numericPrecision + "," + numericScale + ")";
            }
            return attributeType;
        }
    }
}
