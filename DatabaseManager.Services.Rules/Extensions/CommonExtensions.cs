using DatabaseManager.Services.Rules.Models;
using Microsoft.Azure.Functions.Worker.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.Rules.Extensions
{
    public static class CommonExtensions
    {
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

        public static int? GetIntFromString(this string token)
        {
            int? number = null;
            if (!string.IsNullOrWhiteSpace(token))
            {
                int value;
                if (int.TryParse(token, out value)) number = value;
            }
            return number;
        }

        public static string CreateDatabaseConnectionString(this ConnectParametersDto connector)
        {
            string connectionString = string.Empty;
            if (String.IsNullOrEmpty(connector.ConnectionString))
            {
                string source = $"Source={connector.DatabaseServer};";
                string database = $"Initial Catalog ={connector.Catalog};";
                string timeout = "Connection Timeout=120";
                string persistSecurity = "Persist Security Info=False;";
                string multipleActive = "MultipleActiveResultSets=True;";
                string integratedSecurity = "";
                string user = "";
                //Encryption is currently not used, more testing later
                //string encryption = "Encrypt=True;TrustServerCertificate=False;";
                string encryption = "Encrypt=False;";
                if (!string.IsNullOrWhiteSpace(connector.User))
                    user = $"User ID={connector.User};";
                else
                    integratedSecurity = "Integrated Security=True;";
                string password = "";
                if (!string.IsNullOrWhiteSpace(connector.Password)) password = $"Password={connector.Password};";

                connectionString = "Data " + source + persistSecurity + database +
                    user + password + integratedSecurity + encryption + multipleActive;

                connectionString = connectionString + timeout;
            }
            else
            {
                connectionString = connector.ConnectionString;
            }

            return connectionString;
        }
    }
}
