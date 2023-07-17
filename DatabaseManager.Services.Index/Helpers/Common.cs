using DatabaseManager.Services.Index.Models;
using System;
using System.Collections.Generic;
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
                    string splitPattern = @",\n|, ";
                    string[] columnDefinitions = Regex.Split(extractedText, splitPattern);
                    //string[] columnDefinitions = extractedText.Split(',');

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
    }
}
