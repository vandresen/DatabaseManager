using DatabaseManager.Server.Entities;
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
        DataAccessDef dataAccess;
        private string connectionString;
        private DataTable dt;
        SqlDataAdapter dataAdapter;

        public CSVLoader(IWebHostEnvironment env)
        {
            try
            {
                string contentRootPath = env.ContentRootPath;
                string jsonFile = contentRootPath + @"\DataBase\ReferenceTables.json";
                string json = System.IO.File.ReadAllText(jsonFile);
                _references = JsonConvert.DeserializeObject<List<ReferenceTable>>(json);
                jsonFile = contentRootPath + @"\DataBase\PPDMDataAccess.json";
                json = System.IO.File.ReadAllText(jsonFile);
                _dataDef = JsonConvert.DeserializeObject<List<DataAccessDef>>(json);
                jsonFile = contentRootPath + @"\DataBase\CSVDataAccess.json";
                json = System.IO.File.ReadAllText(jsonFile);
                _csvDef = JsonConvert.DeserializeObject<List<CSVAccessDef>>(json);
            }
            catch (Exception ex)
            {
                Exception error = new Exception("Read refernce table file error: ", ex);
                throw error;
            }

        }

        public void LoadCSVFile(string conn, string csvText, string dataType)
        {
            connectionString = conn;
            dataAccess = _dataDef.First(x => x.DataType == dataType);
            InitDataTable(dataType);

            int headerLines = 1;
            int wellCounter = 0;

            CSVAccessDef csvAccess = _csvDef.First(x => x.DataType == "WellBore");
            Dictionary<string, int> attributes = csvAccess.Mappings.ToDictionary();
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
                        //Console.WriteLine("Processing line {0}", wellCounter);
                    }
                    string[] fields = csvParser.ReadFields();
                    string key = GetDataKey(fields, attributes);
                    if (string.IsNullOrEmpty(key))
                    {
                        //Console.WriteLine($"Well {wellCounter} has an empty key");
                    }
                    else
                    {
                        DataRow[] rows = dt.Select(key);
                        if (rows.Length == 1)
                        {
                            rows[0] = InsertCSVRow(rows[0], attributes, fields, columnTypes);
                            rows[0]["ROW_CHANGED_DATE"] = DateTime.Now.ToString("yyyy-MM-dd");
                        }
                        else
                        {
                            DataRow newRow = dt.NewRow();
                            newRow = InsertCSVRow(newRow, attributes, fields, columnTypes);
                            newRow["ROW_CREATED_DATE"] = DateTime.Now.ToString("yyyy-MM-dd");
                            newRow["ROW_CHANGED_DATE"] = DateTime.Now.ToString("yyyy-MM-dd");
                            dt.Rows.Add(newRow);
                        }
                    }
                }
            }
            InsertReferanceData(dt);
            new SqlCommandBuilder(dataAdapter);
            dataAdapter.Update(dt);
        }

        private void InsertReferanceData(DataTable dt)
        {
            List<ReferenceTable> dataTypeRefs = _references.Where(x => x.DataType == "WellBore").ToList();
            foreach (ReferenceTable referance in dataTypeRefs)
            {
                string columnName = referance.ReferenceAttribute;
                List<string> distinctIds = dt.AsEnumerable().Select(row => row.Field<string>(columnName)).Distinct().ToList();

                string select = $"Select * from {referance.Table}";
                SqlDataAdapter refAdapter = new SqlDataAdapter(select, connectionString);
                DataTable rt = new DataTable();
                refAdapter.Fill(rt);

                foreach (string id in distinctIds)
                {
                    if (!string.IsNullOrEmpty(id))
                    {
                        string key = $"{referance.KeyAttribute} = '{id}'";
                        DataRow[] rows = rt.Select(key);
                        if (rows.Length == 0)
                        {

                            DataRow newRow = rt.NewRow();
                            newRow[referance.KeyAttribute] = id;
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
        }


        private string GetDataKey(string[] fields, Dictionary<string, int> attributes)
        {
            string dataKey = "";
            string and = "";
            string[] keys = dataAccess.Keys.Split(',');
            foreach (string key in keys)
            {
                string attribute = key.Trim();
                int column = attributes[attribute];
                if (string.IsNullOrEmpty(fields[column]))
                {
                    dataKey = "";
                    return dataKey;
                }
                string attributeValue = "'" + fields[column] + "'";
                dataKey = dataKey + and + attribute + " = " + attributeValue;
                and = " AND ";
            }
            return dataKey;
        }
        public static DataRow InsertCSVRow(DataRow row, Dictionary<string, int> attributes,
            string[] fields, Dictionary<string, string> columnTypes)
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

            return tmpRow;
        }
    }
}
