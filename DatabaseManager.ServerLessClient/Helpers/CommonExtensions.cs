using DatabaseManager.ServerLessClient.Models;
using Newtonsoft.Json.Linq;

namespace DatabaseManager.ServerLessClient.Helpers
{
    public static class CommonExtensions
    {
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

        public static double[] ConvertStringToArray(this string input)
        {
            double[] output = new double[] { -99999.0 }; ;
            try
            {
                input = input.Trim();
                input = input.TrimStart('[').TrimEnd(']');
                string[] strOutputArray = input.Split(',')
                    .Select(x => x.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToArray();
                output = Array.ConvertAll(strOutputArray, Double.Parse);
            }
            catch (Exception)
            {
            }

            return output;
        }

        public static double GetDoubleFromString(this string token)
        {
            double number = 0.0;
            if (!string.IsNullOrWhiteSpace(token))
            {
                double value;
                if (double.TryParse(token, out value)) number = value;
            }
            return number;
        }

        public static string GetStringFromJson(this string json, string attribute)
        {
            JObject dataObject = JObject.Parse(json);
            JToken token = dataObject.GetValue(attribute);
            string result = "";
            if (token != null)
            {
                result = token.ToString();
            }
            return result;
        }

        public static double? GetNumberFromJson(this string json, string attribute)
        {
            JObject dataObject = JObject.Parse(json);
            JToken token = dataObject.GetValue(attribute);
            double? number = null;
            if (token != null)
            {
                string strNumber = token.ToString();
                if (!string.IsNullOrWhiteSpace(strNumber))
                {
                    double value;
                    if (double.TryParse(strNumber, out value)) number = value;
                }
            }
            return number;
        }

        public static string[] GetAttributes(this string select)
        {
            int from = 7;
            int to = select.IndexOf("from");
            int length = to - 8;
            string attributes = select.Substring(from, length);
            string[] words = attributes.Split(',');

            return words;
        }

        public static string CreateDatabaseConnectionString(this ConnectParameters connection)
        {
            string source = $"Source={connection.DatabaseServer};";
            string database = $"Initial Catalog ={connection.Catalog};";
            string timeout = "Connection Timeout=120";
            string persistSecurity = "Persist Security Info=False;";
            string multipleActive = "MultipleActiveResultSets=True;";
            string integratedSecurity = "";
            string user = "";
            //Encryption is currently not used, more testing later
            //string encryption = "Encrypt=True;TrustServerCertificate=False;";
            string encryption = "Encrypt=False;";
            if (!string.IsNullOrWhiteSpace(connection.User))
                user = $"User ID={connection.User};";
            else
                integratedSecurity = "Integrated Security=True;";
            string password = "";
            if (!string.IsNullOrWhiteSpace(connection.Password)) password = $"Password={connection.Password};";

            string cnStr = "Data " + source + persistSecurity + database +
                user + password + integratedSecurity + encryption + multipleActive;

            cnStr = cnStr + timeout;

            return cnStr;
        }
    }
}
