using DatabaseManager.Services.Index.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DatabaseManager.Services.Index.Helpers
{
    public class Common
    {
        public static List<TableSchema> GetColumnInfo(string tableName, string dataModel)
        {
            List<TableSchema> columns = new();

            string pattern = $"CREATE TABLE {tableName}" + @"[\s\S]*?\);";

            // Find the matching substring
            Match match = Regex.Match(dataModel, pattern);

            if (match.Success)
            {
                string extractedString = match.Value;
                int startIndex = extractedString.IndexOf('(') + 1;
                int endIndex = extractedString.LastIndexOf(')');

                if (startIndex >= 0 && endIndex >= 0 && endIndex > startIndex)
                {
                    string extractedText = extractedString.Substring(startIndex, endIndex - startIndex);
                    //string splitPattern = @",\n|, ";
                    //string[] columnDefinitions = Regex.Split(extractedText, splitPattern);
                    //string[] columnDefinitions = extractedText.Split(',');
                    string[] columnDefinitions = Regex.Split(extractedText, @",(?![^()]*\))");

                    foreach (string columnDefinition in columnDefinitions)
                    {
                        string[] parts = columnDefinition.Trim().Split(' ');

                        if (parts.Length >= 2)
                        {
                            startIndex = parts[1].IndexOf('(') + 1;
                            endIndex = parts[1].LastIndexOf(')');
                            if (startIndex >= 0 && endIndex >= 0 && endIndex > startIndex)
                            {
                                columns.Add(new TableSchema
                                {
                                    COLUMN_NAME = parts[0],
                                    DATA_TYPE = parts[1].Substring(0, startIndex - 1),
                                    CHARACTER_MAXIMUM_LENGTH = parts[1].Substring(startIndex, endIndex - startIndex)
                                });

                            }
                            else
                            {
                                columns.Add(new TableSchema
                                {
                                    COLUMN_NAME = parts[0],
                                    DATA_TYPE = parts[1]
                                });
                            }
                        }
                        else
                        {
                            Console.WriteLine("Invalid column definition: " + columnDefinition);
                        }
                    }
                }
                else
                {
                    //Console.WriteLine("Invalid input string");
                }
            }
            else
            {
                throw new Exception($"Could not get column info, no match for table {tableName}.");
            }
            return columns;
        }

        public static IndexFileData ProcessJTokens(JToken token)
        {
            IndexFileData idxDataObject = new IndexFileData();
            idxDataObject.DataName = (string)token["DataName"];
            idxDataObject.NameAttribute = token["NameAttribute"]?.ToString();
            idxDataObject.LatitudeAttribute = token["LatitudeAttribute"]?.ToString();
            idxDataObject.LongitudeAttribute = token["LongitudeAttribute"]?.ToString();
            idxDataObject.ParentKey = token["ParentKey"]?.ToString();
            if (token["UseParentLocation"] != null) idxDataObject.UseParentLocation = (Boolean)token["UseParentLocation"];
            if (token["Arrays"] != null)
            {
                idxDataObject.Arrays = token["Arrays"];
            }
            return idxDataObject;
        }

        public static List<IndexFileData> ProcessIndexArray(JToken parent, List<IndexFileData> idxData)
        {
            if (parent["DataObjects"] != null)
            {
                foreach (JToken level in parent["DataObjects"])
                {
                    idxData.Add(ProcessJTokens(level));  // Assuming ProcessJTokens returns IndexFileData
                    ProcessIndexArray(level, idxData); // Recursive call
                }
            }
            return idxData;
        }

        public static string GetDataKey(JObject dataObject, string dbKeys)
        {
            string dataKey = "";
            string and = "";
            string[] keys = dbKeys.Split(',');
            foreach (string key in keys)
            {
                string attribute = key.Trim();
                string attributeValue = "'" + dataObject[attribute].ToString() + "'";
                dataKey = dataKey + and + key.Trim() + " = " + attributeValue;
                and = " AND ";
            }
            return dataKey;
        }

        public static double GetLocationFromJson(JObject dataObject, string attribute)
        {
            double location = -99999.0;
            if (!string.IsNullOrEmpty(attribute))
            {
                string strLocation = dataObject[attribute].ToString();
                if (!string.IsNullOrEmpty(strLocation))
                {
                    Boolean isNumber = double.TryParse(strLocation, out location);
                    if (!isNumber) location = -99999.0;
                }
            }
            return location;
        }
    }
}
