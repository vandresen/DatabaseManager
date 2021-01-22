using DatabaseManager.Server.Entities;
using DatabaseManager.Server.Extensions;
using DatabaseManager.Server.Services;
using DatabaseManager.Shared;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.SqlClient;
using Microsoft.VisualBasic.FileIO;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Server.Helpers
{
    public class CSVLoader
    {
        private List<ReferenceTable> _references = new List<ReferenceTable>();
        private List<DataAccessDef> _dataDef = new List<DataAccessDef>();
        private List<CSVAccessDef> _csvDef = new List<CSVAccessDef>();
        private Dictionary<string, string> duplicates =
                       new Dictionary<string, string>();
        DataAccessDef dataAccess;
        private string connectionString;
        private DataTable dt;
        SqlDataAdapter dataAdapter;
        private readonly IFileStorageService fileStorageService;

        public CSVLoader(IFileStorageService fileStorageService)
        {
            this.fileStorageService = fileStorageService;
        }

        public async Task LoadCSVFile(ConnectParameters source, ConnectParameters target, string fileName)
        {
            connectionString = target.ConnectionString;
            string accessJson = await fileStorageService.ReadFile("connectdefinition", "PPDMDataAccess.json");
            _dataDef = JsonConvert.DeserializeObject<List<DataAccessDef>>(accessJson);
            string referenceJson = await fileStorageService.ReadFile("connectdefinition", "PPDMReferenceTables.json");
            _references = JsonConvert.DeserializeObject<List<ReferenceTable>>(referenceJson);
            string csvJson = await fileStorageService.ReadFile("connectdefinition", "CSVDataAccess.json");
            _csvDef = JsonConvert.DeserializeObject<List<CSVAccessDef>>(csvJson);
            string csvText = await fileStorageService.ReadFile(source.Catalog, fileName);
            string dataType = source.DataType.Remove(source.DataType.Length-1, 1);
            dataAccess = _dataDef.First(x => x.DataType == dataType);
            InitDataTable(dataType);

            int headerLines = 1;
            int wellCounter = 0;

            CSVAccessDef csvAccess = _csvDef.First(x => x.DataType == dataType);
            Dictionary<string, int> attributes = csvAccess.Mappings.ToDictionary();
            Dictionary<string, string> constants = csvAccess.Constants.ToStringDictionary();
            Dictionary<string, string> columnTypes = dt.GetColumnTypes();

            byte[] byteArray = Encoding.ASCII.GetBytes(csvText);
            MemoryStream csvStream = new MemoryStream(byteArray);

            using (TextFieldParser csvParser = new TextFieldParser(csvStream))
            {
                csvParser.CommentTokens = new string[] { "#" };
                csvParser.SetDelimiters(new string[] { "," });
                csvParser.HasFieldsEnclosedInQuotes = true;

                for (int i = 0; i < headerLines; i++)
                {
                    string line = csvParser.ReadLine();
                }

                while (!csvParser.EndOfData)
                {
                    wellCounter++;
                    if ((wellCounter % 1000) == 0)
                    {
                        var tmp = wellCounter;
                    }

                    try
                    {
                        string[] fields = csvParser.ReadFields();
                        string key = GetDataKey(fields, attributes);
                        string duplicateKey = GetDuplicateKey(fields, attributes);
                        if (string.IsNullOrEmpty(key))
                        {
                            //Console.WriteLine($"Well {wellCounter} has an empty key");
                        }
                        else
                        {
                            if (duplicates.ContainsKey(duplicateKey))
                            {
                                DataRow[] rows = dt.Select(key);
                                rows[0] = InsertCSVRow(rows[0], attributes, fields, columnTypes, constants);
                                rows[0]["ROW_CHANGED_DATE"] = DateTime.Now.ToString("yyyy-MM-dd");
                            }
                            else
                            {
                                DataRow newRow = dt.NewRow();
                                newRow = InsertCSVRow(newRow, attributes, fields, columnTypes, constants);
                                newRow["ROW_CREATED_DATE"] = DateTime.Now.ToString("yyyy-MM-dd");
                                newRow["ROW_CHANGED_DATE"] = DateTime.Now.ToString("yyyy-MM-dd");
                                dt.Rows.Add(newRow);
                                duplicates.Add(duplicateKey, "");
                            }
                        }
                    }
                    catch (Exception)
                    {

                    }
                }
            }
            InsertReferanceData(dataType);
            new SqlCommandBuilder(dataAdapter);
            dataAdapter.Update(dt);
        }

        private void InsertReferanceData(string dataType)
        {
            List<ReferenceTable> dataTypeRefs = _references.Where(x => x.DataType == dataType).ToList();
            foreach (ReferenceTable referance in dataTypeRefs)
            {
                string columnName = referance.ReferenceAttribute;
                List<string> distinctIds = dt.AsEnumerable().Select(row => row.Field<string>(columnName)).Distinct().ToList();

                string select = $"Select * from {referance.Table}";
                SqlDataAdapter refAdapter = new SqlDataAdapter(select, connectionString);
                DataTable rt = new DataTable();
                refAdapter.Fill(rt);
                int keyAttributeLength = GetAttributeLength(referance.Table, referance.KeyAttribute);
                
                foreach (string id in distinctIds)
                {
                    if (!string.IsNullOrEmpty(id))
                    {
                        string newId = id;
                        string tmpId = "";
                        if (newId.Length > keyAttributeLength)
                        {
                            newId = newId.Substring(0, keyAttributeLength);
                            if (newId.Substring(keyAttributeLength-1, 1) == "'")
                            {
                                newId = newId.Substring(0, keyAttributeLength-1);
                            }
                            tmpId = Common.FixAposInStrings(id);
                            DataRow[] sl = dt.Select($"{columnName} = '{tmpId}'");
                            foreach (DataRow r in sl)
                                r[columnName] = newId;
                        }
                        tmpId = Common.FixAposInStrings(newId);
                        string key = $"{referance.KeyAttribute} = '{tmpId}'";
                        DataRow[] rows = rt.Select(key);
                        if (rows.Length == 0)
                        {
                            DataRow newRow = rt.NewRow();
                            newRow[referance.KeyAttribute] = newId;
                            newRow[referance.ValueAttribute] = id;
                            if (!string.IsNullOrEmpty(referance.FixedKey))
                            {
                                string[] fixedKey = referance.FixedKey.Split('=');
                                newRow[fixedKey[0]] = fixedKey[1];
                            }
                            rt.Rows.Add(newRow);
                        }
                    }
                }
                new SqlCommandBuilder(refAdapter);
                refAdapter.Update(rt);
            }
        }

        private void InitDataTable(string dataType)
        {
            string select = dataAccess.Select;
            dataAdapter = new SqlDataAdapter(select, connectionString);
            dt = new DataTable();
            dataAdapter.Fill(dt);
            InitDuplicateKeys(dataType);
        }

        private void InitDuplicateKeys(string dataType)
        {
            dataAccess = _dataDef.First(x => x.DataType == dataType);
            string[] keys = dataAccess.Keys.Split(',').Select(key => key.Trim()).ToArray();
            foreach (DataRow row in dt.Rows)
            {
                string duplicateKey = "";
                foreach (string key in keys)
                {
                    string value = row[key].ToString();
                    duplicateKey = duplicateKey + value;
                }
                duplicates.Add(duplicateKey.GetSHA256Hash(), "");
            }
        }

        private int GetAttributeLength(string table, string key)
        {
            int length = -1;
            string select = $"Select * from INFORMATION_SCHEMA.COLUMNS where TABLE_NAME = '{table}'";
            SqlDataAdapter schema = new SqlDataAdapter(select, connectionString);
            DataTable st = new DataTable();
            schema.Fill(st);
            select = $"COLUMN_NAME = '{key}'";
            DataRow[] rows = st.Select(select);
            string attributeType = rows[0]["DATA_TYPE"].ToString();
            if (attributeType == "nvarchar")
            {
                length = (int)rows[0]["CHARACTER_MAXIMUM_LENGTH"];
            }
            return length;
        }

        private string GetDataKey(string[] fields, Dictionary<string, int> attributes)
        {
            string dataKey = "";
            string and = "";
            string[] keys = dataAccess.Keys.Split(',');
            foreach (string key in keys)
            {
                string attribute = key.Trim();
                string value = "";
                if (attributes.ContainsKey(attribute)) 
                {
                    int column = attributes[attribute];
                    if (string.IsNullOrEmpty(fields[column]))
                    {
                        dataKey = "";
                        return dataKey;
                    }
                    value = fields[column];
                }
                else
                {
                    value = "UNKNOWN";
                }
                string attributeValue = "'" + value + "'";
                dataKey = dataKey + and + attribute + " = " + attributeValue;
                and = " AND ";
            }
            return dataKey;
        }

        private string GetDuplicateKey(string[] fields, Dictionary<string, int> attributes)
        {
            string dataKey = "";
            //string and = "";
            string[] keys = dataAccess.Keys.Split(',');
            foreach (string key in keys)
            {
                string attribute = key.Trim();
                string value = "";
                if (attributes.ContainsKey(attribute))
                {
                    int column = attributes[attribute];
                    if (string.IsNullOrEmpty(fields[column]))
                    {
                        dataKey = "";
                        return dataKey;
                    }
                    value = fields[column];
                }
                else
                {
                    value = "UNKNOWN";
                }
                dataKey = dataKey + value;
            }
            return dataKey.GetSHA256Hash();
        }

        public static DataRow InsertCSVRow(DataRow row, 
            Dictionary<string, int> attributes,
            string[] fields, 
            Dictionary<string, string> columnTypes,
            Dictionary<string, string> constants)
        {
            DataRow tmpRow = row;
            foreach (var item in attributes)
            {
                string attribute = item.Key;
                int column = item.Value;
                string attributeValue = fields[column];
                if (columnTypes[attribute] == "System.Decimal")
                {
                    decimal number;
                    if (decimal.TryParse(attributeValue, out number))
                    {
                        tmpRow[attribute] = number;
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(attributeValue))
                    {
                        tmpRow[attribute] = attributeValue;
                    }
                }
            }

            foreach(var constant in constants)
            {
                string attribute = constant.Key;
                string attributeValue = constant.Value;
                tmpRow[attribute] = attributeValue;
            }

            return tmpRow;
        }
    }
}
