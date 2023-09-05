using DatabaseManager.Services.DataTransfer.Extensions;
using DatabaseManager.Services.DataTransfer.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DatabaseManager.Services.DataTransfer.Helpers
{
    public class Common
    {
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

        public static DataTable NewDataTable(string dataType, string dataModel, List<DataAccessDef> dataAccessDefs)
        {
            DataAccessDef dataAccess = dataAccessDefs.First(x => x.DataType == dataType);
            string dataTypeSql = dataAccess.Select;
            string table = dataTypeSql.GetTable().ToLower();
            List<TableSchema> attributes = GetColumnInfo(table, dataModel);
            DataTable dt = new DataTable();
            string[] selectAttributes = dataTypeSql.GetSqlSelectAttributes();
            foreach (var item in selectAttributes)
            {
                TableSchema schema = attributes.Find(obj => obj.COLUMN_NAME == item);
                if (schema != null)
                {
                    DataColumn column = null;
                    if (schema.DATA_TYPE == "INT")
                    {
                        column = new DataColumn(schema.COLUMN_NAME, typeof(int));
                    }
                    if (schema.DATA_TYPE == "DATE")
                    {
                        column = new DataColumn(schema.COLUMN_NAME, typeof(DateTime));
                    }
                    if (schema.DATA_TYPE.Contains("NVARCHAR"))
                    {
                        column = new DataColumn(schema.COLUMN_NAME, typeof(string));
                    }
                    if (schema.DATA_TYPE.Contains("NUMERIC"))
                    {
                        column = new DataColumn(schema.COLUMN_NAME, typeof(double));
                    }
                    if (column != null) dt.Columns.Add(column);
                }
            }
            return dt;
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

        public static object ConvertJsonValueToDataTable(JToken valueToken, Type targetType)
        {
            if (valueToken.Type == JTokenType.Null)
            {
                return DBNull.Value;
            }

            // Handle specific data type conversions here
            if (targetType == typeof(int))
            {
                return (int)valueToken;
            }
            else if (targetType == typeof(string))
            {
                return valueToken.ToString();
            }
            else if (targetType == typeof(DateTime))
            {
                return DateTime.Parse(valueToken.ToString());
            }
            // Add more data type conversions as needed

            // If no specific conversion is found, return the value as-is
            return valueToken.ToObject(targetType);
        }
    }
}
