using CsvHelper;
using CsvHelper.Configuration;
using DatabaseManager.Services.DataTransfer.Extensions;
using DatabaseManager.Services.DataTransfer.Helpers;
using DatabaseManager.Services.DataTransfer.Models;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DatabaseManager.Services.DataTransfer.Services
{
    public class CSVTransfer : IDataTransfer
    {
        private List<CSVAccessDef> _csvDef = new List<CSVAccessDef>();
        private List<ReferenceTable> _references = new List<ReferenceTable>();
        private List<DataAccessDef> _dataDef = new List<DataAccessDef>();
        private readonly IFileStorageService _fileStorage;
        DataAccessDef _dataAccess;
        private DataTable _dt = new DataTable();
        IEnumerable<TableSchema> _attributeProperties;
        private readonly IDatabaseAccess _dbAccess;
        private List<dynamic> _newCsvRecords = new List<dynamic>();
        private HashSet<string> _hashSet = new HashSet<string>();

        public CSVTransfer(IFileStorageService fileStorage)
        {
            _fileStorage= fileStorage;
            _dbAccess = new DatabaseAccess();
        }

        public async Task CopyData(TransferParameters transferParameters, ConnectParametersDto sourceConnector, ConnectParametersDto targetConnector, string referenceJson)
        {
            _dataDef = JsonConvert.DeserializeObject<List<DataAccessDef>>(targetConnector.DataAccessDefinition);
            _references = JsonConvert.DeserializeObject<List<ReferenceTable>>(referenceJson);
            _csvDef = JsonConvert.DeserializeObject<List<CSVAccessDef>>(sourceConnector.DataAccessDefinition);
            string csvText = await _fileStorage.ReadFile(sourceConnector.Catalog, sourceConnector.FileName);
            string dataType = sourceConnector.DataType.Remove(sourceConnector.DataType.Length - 1, 1);
            _dataAccess = _dataDef.First(x => x.DataType == dataType);

            CSVAccessDef csvAccess = _csvDef.First(x => x.DataType == dataType);
            Dictionary<string, int> attributes = csvAccess.Mappings.ToDictionary();
            Dictionary<string, string> constants = csvAccess.Constants.ToStringDictionary();
            Dictionary<string, string> columnTypes = _dt.GetColumnTypes();

            string dataTypeSql = _dataAccess.Select;
            string table = dataTypeSql.GetTable();
            _attributeProperties = await _dbAccess.GetColumnInfo(targetConnector.ConnectionString, table);

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
                                TableSchema dataProperty = _attributeProperties.FirstOrDefault(x => x.COLUMN_NAME == dbAttribute);
                                if (dataProperty.DATA_TYPE.Contains("varchar"))
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
                    _dt = DynamicToDT(_newCsvRecords);
                    List<ReferenceTable> dataTypeRefs = _references.Where(x => x.DataType == dataType).ToList();
                    await _dbAccess.InsertDataTableToDatabase(targetConnector.ConnectionString, _dt, 
                        dataTypeRefs, _dataAccess);
                }
            }
        }

        public void DeleteData(ConnectParametersDto source, string table)
        {
            throw new NotImplementedException();
        }

        public async Task<List<string>> GetContainers(ConnectParametersDto source)
        {
            List<string> containers = new List<string>();
            if (string.IsNullOrEmpty(source.FileName))
            {
                Exception error = new Exception($"DataTransfer: Could not get filename for {source.SourceName}");
                throw error;
            }
            containers.Add(source.FileName);
            return containers;
        }

        private void FixKey(DynamicObject newCsvRecord)
        {
            string[] keys = _dataAccess.Keys.Split(',').Select(key => key.Trim()).ToArray();
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
                _newCsvRecords.Add(newCsvRecord);
            }
            else
            {
                _newCsvRecords[index] = newCsvRecord;
            }
        }

        private int CheckForDuplicates(string keyHash)
        {
            int index = -1;
            if (_hashSet.Contains(keyHash))
            {
                DynamicObject foundObject = new DynamicObject();
                index = 0;
                foreach (var row in _newCsvRecords)
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
                _hashSet.Add(keyHash);
            }
            return index;
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
    }
}
