﻿using AutoMapper;
using DatabaseManager.Common.Entities;
using DatabaseManager.Common.Extensions;
using DatabaseManager.Common.Services;
using DatabaseManager.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static DatabaseManager.Common.Helpers.RuleMethodUtilities;

namespace DatabaseManager.Common.Helpers
{
    public class Common
    {
        public static string CreateTempTableSqlFromJson(string json)
        {
            string sql = "";
            JObject jsonObject = JObject.Parse(json);
            Dictionary<string, Type> columnDefinitions = new Dictionary<string, Type>();
            foreach (var property in jsonObject.Properties())
            {
                JTokenType propertyType = property.Value.Type;

                // Map JSON types to .NET types (you may need to add more cases as needed)
                Type columnType;
                switch (propertyType)
                {
                    case JTokenType.String:
                        columnType = typeof(string);
                        break;
                    case JTokenType.Integer:
                        columnType = typeof(int);
                        break;
                    case JTokenType.Float:
                        columnType = typeof(double);
                        break;
                    case JTokenType.Boolean:
                        columnType = typeof(bool);
                        break;
                    default:
                        columnType = typeof(string); // Default to string if the type is not recognized
                        break;
                }

                columnDefinitions.Add(property.Name, columnType);
            }

            // Create the temporary table in the SQL Server database
            string tempTableName = "#TempTable";
            string createTableQuery = $"CREATE TABLE {tempTableName} (";

            foreach (var column in columnDefinitions)
            {
                createTableQuery += $"{column.Key} {GetSqlDbTypeString(column.Value)}, ";
            }

            sql = createTableQuery.TrimEnd(',', ' ') + ")";

            return sql;
        }

        public static string GetSqlDbTypeString(Type dataType)
        {
            if (dataType == typeof(string))
                return "NVARCHAR(100)"; // Adjust the length as needed
            if (dataType == typeof(int))
                return "INT";
            if (dataType == typeof(double))
                return "FLOAT";
            if (dataType == typeof(bool))
                return "BIT";

            return "NVARCHAR(100)"; // Default to NVARCHAR if type is not recognized
        }

        public static string CreateDatabaseConnectionString(ConnectParameters connection)
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

        public static async Task<ConnectParameters> GetConnectParameters(string azureConnectionString, string connecterSource)
        {
            if (String.IsNullOrEmpty(azureConnectionString))
            {
                Exception error = new Exception($"Azure Connection string is not set");
                throw error;
            }
            Sources so = new Sources(azureConnectionString);
            ConnectParameters connector = await so.GetSourceParameters(connecterSource);
            return connector;
        }

        public static string ConvertDataRowToJson(DataRow dataRow, DataTable dt)
        {
            DataTable tmp = new DataTable();
            tmp = dt.Clone();
            tmp.Rows.Add(dataRow.ItemArray);
            string jsonData = JsonConvert.SerializeObject(tmp);
            jsonData = jsonData.Replace("[", "");
            jsonData = jsonData.Replace("]", "");
            return jsonData;
        }

        public static string FixAposInStrings(string st)
        {
            string fixString = st;
            int length;
            int start = 0;
            int end = st.IndexOf("'");
            while (end >= 0)
            {
                length = end;
                string s1 = fixString.Substring(0, length);
                string s2 = fixString.Substring(end);
                fixString = s1 + "'" + s2;
                start = end + 2;
                end = fixString.IndexOf("'", start);
            }
            return fixString;
        }

        public static string[] GetAttributes(string select)
        {
            int from = 7;
            int to = select.IndexOf("from");
            int length = to - 8;
            string attributes = select.Substring(from, length);
            string[] words = attributes.Split(',')
                .Select(item => item.Trim())
                .ToArray();
            return words;
        }

        public static T[] GetArrayFromString<T>(string input)
        {
            string[] stringArray = input.Split(',');
            T[] resultArray = stringArray.Select(item => (T)Convert.ChangeType(item.Trim(), typeof(T))).ToArray();
            return resultArray;
        }

        public static string GetTable(string select)
        {
            select = select.ToUpper();
            int from = select.IndexOf(" FROM ") + 6;
            string table = select.Substring(from);
            return table;
        }

        public static string SetJsonDataObjectDate(string jsonText, string attribute)
        {
            string jsonDate = DateTime.Now.ToString("yyyy-MM-dd");
            JObject dataObject = JObject.Parse(jsonText);
            dataObject[attribute] = jsonDate;
            jsonText = dataObject.ToString();
            return jsonText;
        }

        public static bool Between(double x, double min, double max)
        {
            return (min < x) && (x < max);
        }

        public static double GetDataRowNumber(DataRow dr, string attribute)
        {
            double number = -99999.0;
            if (!string.IsNullOrEmpty(attribute))
            {
                string strNumber = dr[attribute].ToString();
                if (!string.IsNullOrEmpty(strNumber))
                {
                    Boolean isNumber = double.TryParse(strNumber, out number);
                    if (!isNumber) number = -99999.0;
                }
            }
            return number;
        }

        public static double GetLogNullValue(string jsonData)
        {
            double nullValue = -999.2500;
            JObject json = JObject.Parse(jsonData);
            JToken jsonToken = json["NULL_REPRESENTATION"];
            if (jsonToken is null)
            {
                Console.WriteLine("Error: NULL value is null");
            }
            else
            {
                if (double.TryParse(jsonToken.ToString(), out double value))
                {
                    nullValue = value;
                }
                else
                {
                    Console.WriteLine("Error: Not a proper null number");
                }

            }
            return nullValue;
        }

        public static string CompletenessCheck(string strValue)
        {
            string status = "Passed";
            if (string.IsNullOrWhiteSpace(strValue))
            {
                status = "Failed";
            }
            else
            {
                double number;
                bool canConvert = double.TryParse(strValue, out number);
                if (canConvert)
                {
                    if (number == -99999) status = "Failed";
                }
            }
            return status;
        }

        public static double CalculateStdDev(List<double> values)
        {
            double stdDev = 0;
            if (values.Count > 2)
            {
                double average = values.Average();
                double sum = values.Sum(d => Math.Pow(d - average, 2));
                stdDev = Math.Sqrt((sum) / (values.Count() - 1));
            }
            return stdDev;
        }

        public static string GetCreateSQLFromDataTable(string tableName, DataTable schema)
        {
            string sql = "CREATE TABLE [" + tableName + "] (\n";

            // columns
            foreach (DataColumn column in schema.Columns)
            {
                string columnName = column.ColumnName;
                string columnType = DataTableToSQLConvertType(column);
                sql += "\t[" + columnName + "] " + columnType;
                sql += ",\n";
            }
            sql = sql.TrimEnd(new char[] { ',', '\n' }) + "\n";

            sql += ")";

            return sql;
        }

        public static string DataTableToSQLConvertType(DataColumn column)
        {
            string type = column.DataType.Name;
            int columnSize = column.MaxLength;
            switch (type)
            {
                case "String":
                    return "VARCHAR(" + ((columnSize == -1) ? "255" : (columnSize > 8000) ? "MAX" : columnSize.ToString()) + ")";

                case "Decimal":
                    return "REAL";

                case "Double":
                case "Single":
                    return "REAL";

                case "Int64":
                    return "BIGINT";

                case "Int16":
                case "Int32":
                    return "INT";

                case "DateTime":
                    return "DATETIME";

                case "Boolean":
                    return "BIT";

                case "Byte":
                    return "TINYINT";

                default:
                    throw new Exception(type.ToString() + " not implemented.");
            }
        }

        public static string GetStorageKey(HttpRequest req)
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

        public static string GetQueryString(HttpRequest req, string queryAttribute)
        {
            string queryResult = req.Query[queryAttribute];
            if (string.IsNullOrEmpty(queryResult))
            {
                Exception error = new Exception($"Error getting http query results for attribute {queryAttribute}");
                throw error;
            }
            return queryResult;
        }

        public static int GetIntFromWebQuery(HttpRequest req, string queryAttribute)
        {
            string strId = req.Query[queryAttribute];
            int? tmpId = strId.GetIntFromString();
            if (tmpId == null)
            {
                Exception error = new Exception($"Error getting integer from the web query");
                throw error;
            }
            int responseInt = tmpId.GetValueOrDefault();
            return responseInt;
        }

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

        public static string CreateJsonForNewDataObject(DataAccessDef accessDef, IEnumerable<TableSchema> attributeProperties)
        {
            string json = "{}";
            string[] columns = Common.GetAttributes(accessDef.Select);
            JObject dataObject = JObject.Parse(json);
            foreach (string column in columns)
            {
                TableSchema tableSchema = attributeProperties.FirstOrDefault(x => x.COLUMN_NAME == column.Trim());
                if (tableSchema == null)
                {
                    break;
                }
                else
                {
                    string type = tableSchema.DATA_TYPE.ToLower();
                    if (type.Contains("numeric"))
                    {
                        dataObject[column.Trim()] = -99999.0;
                    }
                    else if (type.Contains("date"))
                    {
                        dataObject[column.Trim()] = DateTime.Now.ToString("yyyy-MM-dd");
                    }
                    else
                    {
                        dataObject[column.Trim()] = "";
                    }
                }
            }
            json = dataObject.ToString();

            return json;
        }

        public static string UpdateJsonAttribute<T>(string json, string attributeName, T newValue)
        {
            JObject jsonObject = JObject.Parse(json);
            jsonObject[attributeName] = JToken.FromObject(newValue);
            return jsonObject.ToString(Formatting.None);
        }
    }
}
