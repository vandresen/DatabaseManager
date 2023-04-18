using Microsoft.Azure.Functions.Worker.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.DataTransfer.Extensions
{
    public static class CommonExtensions
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

        public static string GetQuery(this HttpRequestData req, string queryAttribute, bool mandatory)
        {
            var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            string result = query[queryAttribute];
            if (string.IsNullOrEmpty(result) && mandatory)
            {
                Exception error = new Exception($"Error getting query result for {queryAttribute}");
                throw error;
            }
            return result;
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
    }
}
