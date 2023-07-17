using CsvHelper;
using CsvHelper.Configuration;
using DatabaseManager.Services.Index.Extensions;
using DatabaseManager.Services.Index.Models;
using DatabaseManager.Services.Index.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DatabaseManager.Services.Index.Helpers
{
    public class CSVLoader
    {
        private List<ReferenceTable> _references = new List<ReferenceTable>();
        private List<DataAccessDef> _dataDef = new List<DataAccessDef>();
        private List<CSVAccessDef> _csvDef = new List<CSVAccessDef>();
        private List<dynamic> newCsvRecords = new List<dynamic>();
        private HashSet<string> hashSet = new HashSet<string>();
        List<TableSchema> attributeProperties;
        private readonly IFileStorageService _fileStorageService;
        private DataTable dt = new DataTable();
        DataAccessDef dataAccess;
        private string tableDataType;
        string ppdmModel;

        public CSVLoader(IFileStorageService fileStorageService)
        {
            _fileStorageService = fileStorageService;
        }

        public async Task<DataTable> GetCSVTable(ConnectParametersDto source, ConnectParametersDto target, string indexDataType)
        {
            DataTable result = new DataTable();

            if (dt.Rows.Count == 0)
            {
                dt = new DataTable();
                string accessJson = await _fileStorageService.ReadFile("connectdefinition", "PPDMDataAccess.json");
                _dataDef = JsonConvert.DeserializeObject<List<DataAccessDef>>(accessJson);
                string referenceJson = await _fileStorageService.ReadFile("connectdefinition", "PPDMReferenceTables.json");
                _references = JsonConvert.DeserializeObject<List<ReferenceTable>>(referenceJson);
                string csvJson = await _fileStorageService.ReadFile("connectdefinition", "CSVDataAccess.json");
                _csvDef = JsonConvert.DeserializeObject<List<CSVAccessDef>>(csvJson);
                string csvText = await _fileStorageService.ReadFile(source.Catalog, source.FileName);
                ppdmModel = await _fileStorageService.ReadFile("ppdm39", "TAB.sql");

                string dataType = source.DataType.Remove(source.DataType.Length - 1, 1);
                dataAccess = _dataDef.First(x => x.DataType == dataType);
                tableDataType = dataType;

                CSVAccessDef csvAccess = _csvDef.First(x => x.DataType == dataType);
                Dictionary<string, int> attributes = csvAccess.Mappings.ToDictionary();
                Dictionary<string, string> constants = csvAccess.Constants.ToStringDictionary();
                Dictionary<string, string> columnTypes = dt.GetColumnTypes();

                string dataTypeSql = dataAccess.Select;
                string table = dataTypeSql.GetTable().ToLower();
                attributeProperties = Common.GetColumnInfo(table, ppdmModel);

                using (TextReader csvStream = new StringReader(csvText))
                {
                    var conf = new CsvConfiguration(CultureInfo.InvariantCulture)
                    {
                        //BadDataFound = null
                    };
                    using (var csv = new CsvReader(csvStream, conf))
                    {
                        csv.Read();
                        csv.ReadHeader();
                        string[] headerRow = csv.HeaderRecord;
                        var attributeMappings = new Dictionary<string, string>();
                        foreach (var item in attributes)
                        {
                            int colNumber = item.Value;
                            attributeMappings.Add(headerRow[colNumber], item.Key);
                        }

                        List<dynamic> csvRecords = csv.GetRecords<dynamic>().ToList();
                        foreach (var row in csvRecords)
                        {
                            DynamicObject newCsvRecord = new DynamicObject();
                            foreach (var item in row)
                            {
                                if (attributeMappings.ContainsKey(item.Key))
                                {
                                    string dbAttribute = attributeMappings[item.Key];
                                    string value = item.Value;
                                    TableSchema dataProperty = attributeProperties.FirstOrDefault(x => x.COLUMN_NAME == dbAttribute);
                                    if (dataProperty.DATA_TYPE.ToLower().Contains("varchar"))
                                    {
                                        string numberString = Regex.Match(dataProperty.CHARACTER_MAXIMUM_LENGTH, @"\d+").Value;
                                        int maxCharacters = Int32.Parse(numberString);
                                        if (value.Length > maxCharacters)
                                        {
                                            value = value.Substring(0, maxCharacters);
                                        }
                                    }
                                    newCsvRecord.AddProperty(dbAttribute, value);
                                }
                            }
                            FixKey(newCsvRecord);
                        }
                        dt = DynamicToDT(newCsvRecords);
                    }
                }
            }

            if (tableDataType == indexDataType)
            {
                result = dt;
            }
            else
            {
                result = GetParentDataTable(indexDataType);
            }

            return result;
        }

        private DataTable DynamicToDT(List<dynamic> objects)

        {
            var data = objects.ToArray();
            if (data.Count() == 0) return null;
            var dt = new DataTable();
            var item = data[0];
            List<string> keys = item.GetKeys();
            foreach (var key in keys)
            {
                dt.Columns.Add(key);
            }

            foreach (var d in data)
            {
                dt.Rows.Add(d.GetValueArray());
            }
            dt.Columns.Remove("KeyHash");
            return dt;
        }

        private void FixKey(DynamicObject newCsvRecord)
        {
            string[] keys = dataAccess.Keys.Split(',').Select(key => key.Trim()).ToArray();
            string keyText = "";
            foreach (string key in keys)
            {
                if (newCsvRecord.PropertyExist(key))
                {
                    string propValue = newCsvRecord.GetProperty(key);
                    if (string.IsNullOrEmpty(propValue))
                    {
                        newCsvRecord.ChangeProperty(key, "UNKNOWN");
                    }
                }
                else
                {
                    newCsvRecord.AddProperty(key, "UNKNOWN");
                }
                keyText = keyText + newCsvRecord.GetProperty(key);
            }
            keyText = keyText.ToUpper();
            string keyHash = keyText.GetSHA256Hash();
            newCsvRecord.AddProperty("KeyHash", keyHash);
            int index = CheckForDuplicates(keyHash);
            if (index == -1)
            {
                newCsvRecords.Add(newCsvRecord);
            }
            else
            {
                newCsvRecords[index] = newCsvRecord;
            }
        }

        private int CheckForDuplicates(string keyHash)
        {
            int index = -1;
            if (hashSet.Contains(keyHash))
            {
                DynamicObject foundObject = new DynamicObject();
                index = 0;
                foreach (var row in newCsvRecords)
                {
                    string key = row.GetProperty("KeyHash").ToString();
                    if (key == keyHash)
                    {

                        foundObject = row;
                        break;
                    }
                    index++;
                }
            }
            else
            {
                hashSet.Add(keyHash);
            }
            return index;
        }

        private DataTable GetParentDataTable(string dataType)
        {
            DataTable pt = new DataTable();
            dataAccess = _dataDef.First(x => x.DataType == dataType);
            string select = dataAccess.Select;
            string table = select.GetTable();
            List<ReferenceTable> dataTypeRefs = _references.Where(x => x.DataType == tableDataType).ToList();
            foreach (ReferenceTable referance in dataTypeRefs)
            {
                if (referance.Table == table)
                {
                    string columnName = referance.ReferenceAttribute;
                    List<string> distinctIds = dt.AsEnumerable().Select(row => row.Field<string>(columnName)).Distinct().ToList();
                    pt = NewDataTable(dataType);
                    int keyAttributeLength = GetAttributeLength(referance.Table, referance.KeyAttribute);
                    foreach (string id in distinctIds)
                    {
                        if (!string.IsNullOrEmpty(id))
                        {
                            string newId = id;
                            if (newId.Length > keyAttributeLength)
                            {
                                newId = newId.Substring(0, keyAttributeLength);
                                if (newId.Substring(keyAttributeLength - 1, 1) == "'")
                                {
                                    newId = newId.Substring(0, keyAttributeLength - 1);
                                }

                                DataRow[] sl = dt.Select($"{columnName} = '{id}'");
                                foreach (DataRow r in sl)
                                    r[columnName] = newId;
                            }
                            string tmpId = newId.FixAposInStrings();
                            string key = $"{referance.KeyAttribute} = '{tmpId}'";
                            DataRow[] rows = pt.Select(key);
                            if (rows.Length == 0)
                            {
                                DataRow newRow = pt.NewRow();
                                newRow[referance.KeyAttribute] = newId;
                                newRow[referance.ValueAttribute] = id;
                                if (!string.IsNullOrEmpty(referance.FixedKey))
                                {
                                    string[] fixedKey = referance.FixedKey.Split('=');
                                    newRow[fixedKey[0]] = fixedKey[1];
                                }
                                pt.Rows.Add(newRow);
                            }
                        }
                    }
                }
            }
            return pt;
        }

        private DataTable NewDataTable(string dataType)
        {
            dataAccess = _dataDef.First(x => x.DataType == dataType);
            string dataTypeSql = dataAccess.Select;
            string table = dataTypeSql.GetTable().ToLower();
            List<TableSchema> attributes = Common.GetColumnInfo(table, ppdmModel);
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

        private int GetAttributeLength(string table, string key)
        {
            int length = -1;
            table = table.ToLower();
            List<TableSchema> attributes = Common.GetColumnInfo(table, ppdmModel);
            TableSchema schema = attributes.Find(obj => obj.COLUMN_NAME == key);
            if (schema != null)
            {
                if (schema.DATA_TYPE.Contains("NVARCHAR"))
                {
                    int? tmpLenght = schema.CHARACTER_MAXIMUM_LENGTH.GetIntFromString();
                    if (tmpLenght != null) length = (int)tmpLenght;
                }
            }
            return length;
        }
    }
}
