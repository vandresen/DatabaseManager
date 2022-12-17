using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.BlazorComponents.Extensions
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
    }
}
