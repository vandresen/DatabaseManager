using DatabaseManager.Services.DataTransfer.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using DatabaseManager.Services.DataTransfer.Extensions;
using System.Data;
using Microsoft.Data.SqlClient;

namespace DatabaseManager.Services.DataTransfer.Services
{
    public class LASTransfer : IDataTransfer
    {
        private string _uwi;
        private string _nullRepresentation;
        private readonly IFileStorageService _fileStorage;
        private List<DataAccessDef> _dataDef = new List<DataAccessDef>();
        private List<ReferenceTable> _references = new List<ReferenceTable>();
        private LASMappings _lasMappings = new LASMappings();
        private List<LASSections> lasSections = new List<LASSections>();
        private List<double> _curveValues = new List<double>();
        private List<string> _logNames = new List<string>();
        private List<double> _indexValues = new List<double>();
        private List<LASLine> _mnemInfo;
        private List<string> _logCurveList;
        private List<string> _logList;
        private string _logSource;
        private string _connectionString;
        private string _dbUserName;
        private readonly IDatabaseAccess _dbAccess;

        public LASTransfer(IFileStorageService fileStorage)
        {
            _fileStorage = fileStorage;
            _dbAccess = new DatabaseAccess();
            _nullRepresentation = "-999.25";
            _mnemInfo = new List<LASLine>();
        }
        public async Task CopyData(TransferParameters transferParameters, ConnectParametersDto sourceConnector, ConnectParametersDto targetConnector, string referenceJson)
        {
            _logSource = transferParameters.Table;
            _connectionString = targetConnector.ConnectionString;
            _dataDef = JsonConvert.DeserializeObject<List<DataAccessDef>>(targetConnector.DataAccessDefinition);
            _references = JsonConvert.DeserializeObject<List<ReferenceTable>>(referenceJson);
            _lasMappings = JsonConvert.DeserializeObject<LASMappings>(sourceConnector.DataAccessDefinition);

            LASSections ls = await GetLASSections(sourceConnector.Catalog, transferParameters.Table);
            _dbUserName = await _dbAccess.GetUserName(targetConnector.ConnectionString);

            GetVersionInfo(ls.versionInfo);
            string json = await GetHeaderInfo(ls.wellInfo, targetConnector.ConnectionString);
            await LoadHeader(json);
            GetCurveInfo(ls.curveInfo);
            GetDataInfo(ls.dataInfo);
            await LoadParameterInfo();
            LoadLogs();
        }

        public void DeleteData(ConnectParametersDto source, string table)
        {
            throw new NotImplementedException();
        }

        public async Task<List<string>> GetContainers(ConnectParametersDto source)
        {
            List<string> files = new List<string>();
            _fileStorage.SetConnectionString(source.ConnectionString);
            files = await _fileStorage.ListFiles(source.Catalog);
            return files;
        }

        private async Task<LASSections> GetLASSections(string fileShare, string file)
        {
            LASSections lasSections = new LASSections();
            string fileText = await _fileStorage.ReadFile(fileShare, file);
            char[] charSeparators = new char[] { '~' };
            string[] sections = fileText.Split(charSeparators, StringSplitOptions.RemoveEmptyEntries);
            foreach (string section in sections)
            {
                string flag = section.Substring(0, 1);
                if (flag == "V") lasSections.versionInfo = section;
                if (flag == "W") lasSections.wellInfo = section;
                if (flag == "C") lasSections.curveInfo = section;
                if (flag == "P") lasSections.parameterInfo = section;
                if (flag == "A") lasSections.dataInfo = section;
            }
            return lasSections;
        }

        private void GetVersionInfo(string versionInfo)
        {
            string input = null;
            bool versionFound = false;

            StringReader sr = new StringReader(versionInfo);
            while ((input = sr.ReadLine()) != null)
            {
                LASLine line = DecodeLASLine(input);
                if (line.Mnem == "VERS")
                {
                    if (line.Data.Substring(0, 3) != "2.0")
                    {
                        throw new System.Exception("LAS file version not supported");
                    }
                    versionFound = true;
                }
                if (line.Mnem == "WRAP")
                {
                    if (line.Data != "NO")
                    {
                        throw new System.Exception("LAS file wrap is not supported");
                    }
                }
            }
            if (!versionFound)
            {
                throw new System.Exception("LAS file is missing the version tag");
            }
        }

        private static LASLine DecodeLASLine(string input)
        {
            LASLine line = new LASLine();
            string flag = input.Substring(0, 1);
            if (flag != "#")
            {
                int firstDot = input.IndexOf(".");
                if (firstDot > 0)
                {
                    line.Mnem = input.Substring(0, firstDot);
                    line.Mnem = line.Mnem.Trim();
                    input = input.Substring(firstDot + 1);
                    int firstSpace = input.IndexOf(" ");
                    line.Unit = "";
                    if (firstSpace > 0)
                    {
                        line.Unit = input.Substring(0, firstSpace);
                        input = input.Substring(firstSpace);
                    }
                    int lastColon = input.LastIndexOf(":");
                    if (lastColon > 0)
                    {
                        line.Data = input.Substring(0, lastColon);
                        line.Data = line.Data.Trim();
                        input = input.Substring(lastColon + 1);
                    }
                    line.Description = input;
                    line.Description = line.Description.Trim();
                    return line;
                }
            }

            return line;
        }

        private async Task<string> GetHeaderInfo(string wellInfo, string connectionString)
        {
            LASHeaderMappings headMap = new LASHeaderMappings();
            DataAccessDef dataType = _dataDef.First(x => x.DataType == "WellBore");
            string input = null;
            Dictionary<string, string> header = new Dictionary<string, string>();
            string[] attributes = dataType.Select.GetAttributes();
            string table = dataType.Select.GetTable();
            IEnumerable<TableSchema> attributeProperties = await _dbAccess.GetColumnInfo(connectionString, table);
            foreach (string attribute in attributes)
            {
                header.Add(attribute.Trim(), "");
            }
            header["ASSIGNED_FIELD"] = "UNKNOWN";
            header["OPERATOR"] = "UNKNOWN";
            header["DEPTH_DATUM"] = "UNKNOWN";
            header["CURRENT_STATUS"] = "UNKNOWN";
            header["ROW_CREATED_BY"] = _dbUserName;
            header["ROW_CHANGED_BY"] = _dbUserName;
            header.Add("API", "");
            StringReader sr = new StringReader(wellInfo);
            List<LASLine> headerMnems = new List<LASLine>();
            while ((input = sr.ReadLine()) != null)
            {
                LASLine line = DecodeLASLine(input);
                if (!string.IsNullOrEmpty(line.Mnem))
                {
                    headerMnems.Add(line);
                }
            }
            foreach (var line in headerMnems)
            {
                WellMapping mapping = _lasMappings.WellMappings.FirstOrDefault(s => s.LASMnem == line.Mnem);
                if (mapping != null)
                {
                    string value = mapping.DBMnem;
                    header[value] = line.Data;
                    if (value == "NULL") _nullRepresentation = line.Data;
                }
            }
            foreach (var alternativeKey in _lasMappings.AlernativeKeys)
            {
                if (!string.IsNullOrEmpty(header["UWI"])) break;
                string[] keys = alternativeKey.Key.Split(',');
                string seperator = "";
                foreach (var key in keys)
                {
                    LASLine line = headerMnems.FirstOrDefault(s => s.Mnem == key.Trim());
                    if (line != null)
                    {
                        if (!string.IsNullOrEmpty(line.Data))
                        {
                            header["UWI"] = seperator + line.Data;
                            seperator = "-";
                        }
                    }
                }
            }
            foreach (string item in attributes)
            {
                string attribute = item.Trim();
                TableSchema dataProperty = attributeProperties.FirstOrDefault(x => x.COLUMN_NAME == attribute);
                if (dataProperty.DATA_TYPE.Contains("varchar"))
                {
                    int? length = Regex.Match(dataProperty.CHARACTER_MAXIMUM_LENGTH, @"\d+").Value.GetIntFromString();
                    if (length != null) header[attribute] = header[attribute].Truncate(length.GetValueOrDefault());
                }
            }
            string json = JsonConvert.SerializeObject(header, Formatting.Indented);
            _uwi = header["UWI"];
            return json;
        }

        private async Task LoadHeader(string json)
        {
            List<ReferenceTable> dataTypeRefs = _references.Where(x => x.DataType == "WellBore").ToList();
            foreach (ReferenceTable reference in dataTypeRefs)
            {
                json = await CheckHeaderForeignKeys(json, reference);
            }
            DataAccessDef dataType = _dataDef.First(x => x.DataType == "WellBore");
            string tmpUwi = _uwi.FixAposInStrings();
            string table = dataType.Select.GetTable();
            string query = $"Select count(*) from {table} where UWI = '{tmpUwi}'";
            int count = await _dbAccess.GetCount(_connectionString, query);
            if (count == 0)
            {
                json = json.SetJsonDataObjectDate("ROW_CREATED_DATE");
                json = json.SetJsonDataObjectDate("ROW_CHANGED_DATE");
                JObject jo = JObject.Parse(json);
                foreach (JProperty property in jo.Properties())
                {
                    string strValue = property.Value.ToString();
                    if (string.IsNullOrEmpty(strValue))
                    {
                        property.Value = null;
                    }
                }
                json = jo.ToString();
                await _dbAccess.SaveData("dbo.spInsertWellBore", new { json = json }, _connectionString);
            }
        }

        private async Task<string> CheckHeaderForeignKeys(string json, ReferenceTable reference)
        {
            try
            {
                JObject dataObject = JObject.Parse(json);
                string field = dataObject[reference.ReferenceAttribute].ToString();
                field = field.FixAposInStrings();
                string query = $"Select count(*) from {reference.Table} where {reference.KeyAttribute} = '{field}'";
                int count = await _dbAccess.GetCount(_connectionString, query);
                if (count == 0)
                {
                    if (reference.Insert)
                    {
                        string strInsert = $"insert into {reference.Table} ";
                        string strValue = $" ({reference.KeyAttribute}, {reference.ValueAttribute}) values ('{field}', '{field}')";
                        string strQuery = "";
                        string sql = strInsert+ strValue + strQuery ;
                        await _dbAccess.InsertData(_connectionString, sql);
                    }
                    else
                    {
                        dataObject[reference.ReferenceAttribute] = "UNKNOWN";
                    }
                }
            }
            catch (Exception)
            {
                throw new System.Exception("Error handling reference data");
            }

            string newJson = json;
            return newJson;
        }

        private void GetCurveInfo(string curveInfo)
        {
            string input = null;
            StringReader sr = new StringReader(curveInfo);
            while ((input = sr.ReadLine()) != null)
            {
                LASLine line = DecodeLASLine(input);
                if (!string.IsNullOrEmpty(line.Mnem))
                {
                    _logNames.Add(line.Mnem.Trim());
                    _mnemInfo.Add(new LASLine
                    {
                        Mnem = line.Mnem.Trim(),
                        Unit = line.Unit.Trim(),
                        Data = line.Data.Trim(),
                        Description = line.Description
                    });
                }
            }
        }

        private void GetDataInfo(string dataInfo)
        {
            string input = null;
            double value;
            StringReader sr = new StringReader(dataInfo);
            input = sr.ReadLine();
            while ((input = sr.ReadLine()) != null)
            {
                string cValues = input;
                for (int j = 0; j < _logNames.Count; j++)
                {
                    cValues = cValues.Trim();
                    int space = cValues.IndexOf(" ");
                    string cValue = string.Empty;
                    if (space > -1)
                    {
                        cValue = cValues.Substring(0, space);
                    }
                    else
                    {
                        cValue = cValues;
                    }
                    bool canConvert = double.TryParse(cValue, out value);
                    if (!canConvert)
                    {
                        value = -999.25;
                    }
                    _curveValues.Add(value);
                    if (space > -1) cValues = cValues.Substring(space);
                }
            }
        }

        private async Task LoadParameterInfo()
        {
            DataAccessDef dataType = _dataDef.FirstOrDefault(x => x.DataType == "LogParameter");
            if (dataType != null)
            {
                LASSections ls = lasSections[0];
                string input = null;
                if (!string.IsNullOrEmpty(ls.parameterInfo))
                {
                    bool exist = await LogParmExist();
                    if (!exist)
                    {
                        DataRow newRow;
                        DataTable dtNew = new DataTable();
                        string logParmtable = dataType.Select.GetTable();
                        string sqlQuery = $"select * from {logParmtable} where 0 = 1";
                        using (SqlDataAdapter logParmAdapter = new SqlDataAdapter(sqlQuery, _connectionString))
                        {
                            logParmAdapter.Fill(dtNew);
                        }
                        int seqNo = 0;
                        StringReader sr = new StringReader(ls.parameterInfo);
                        while ((input = sr.ReadLine()) != null)
                        {
                            LASLine line = DecodeLASLine(input);
                            if (!string.IsNullOrEmpty(line.Mnem))
                            {
                                seqNo++;
                                newRow = dtNew.NewRow();
                                newRow["UWI"] = _uwi;
                                newRow["WELL_LOG_ID"] = _logSource;
                                newRow["WELL_LOG_SOURCE"] = "Source";
                                newRow["PARAMETER_SEQ_NO"] = seqNo;
                                newRow["PARAMETER_TEXT_VALUE"] = line.Data.Trim();
                                newRow["REPORTED_DESC"] = line.Description.Trim();
                                newRow["REPORTED_MNEMONIC"] = line.Mnem.Trim();
                                newRow["ROW_CREATED_BY"] = _dbUserName;
                                newRow["ROW_CHANGED_BY"] = _dbUserName;
                                newRow["ROW_CREATED_DATE"] = DateTime.Now.ToString("yyyy-MM-dd");
                                newRow["ROW_CHANGED_DATE"] = DateTime.Now.ToString("yyyy-MM-dd");
                                dtNew.Rows.Add(newRow);
                            }
                        }

                        if (dtNew.Rows.Count > 0)
                        {
                            using (SqlConnection destinationConnection = new SqlConnection(_connectionString))
                            {
                                destinationConnection.Open();
                                using (SqlBulkCopy bulkCopy =
                                new SqlBulkCopy(destinationConnection.ConnectionString))
                                {
                                    bulkCopy.BatchSize = 500;
                                    bulkCopy.DestinationTableName = logParmtable;
                                    bulkCopy.WriteToServer(dtNew);
                                }
                            }
                        }


                    }

                }
            }
        }

        private async Task<bool> LogParmExist()
        {
            bool exist = false;

            DataAccessDef dataType = _dataDef.First(x => x.DataType == "LogParameter");
            string table = dataType.Select.GetTable();
            string tmpUwi = _uwi.FixAposInStrings();
            string query = $"Select count(*) from {table} where UWI = '{tmpUwi}' and WELL_LOG_ID = '{_logSource}'";
            int count = await _dbAccess.GetCount(_connectionString, query);
            if (count > 0) exist = true;

            return exist;
        }

        private void LoadLogs()
        {
            DataRow newRow;
            DataTable dtNew = new DataTable();
            DataAccessDef dataType = _dataDef.First(x => x.DataType == "LogCurve");
            string select = dataType.Select;
            string logCurvetable = select.GetTable();
            string sqlQuery = $"select * from {logCurvetable} where 0 = 1";
            using (SqlDataAdapter logCurveValueAdapter = new SqlDataAdapter(sqlQuery, _connectionString))
            {
                logCurveValueAdapter.Fill(dtNew);
            }
                
            _logCurveList = GetLogCurveList();
            _logList = GetLogList();

            DataTable lgNew = new DataTable();
            dataType = _dataDef.First(x => x.DataType == "Log");
            select = dataType.Select;
            string logTable = select.GetTable();
            sqlQuery = $"select * from {logTable} where 0 = 1";
            using (SqlDataAdapter logValueAdapter = new SqlDataAdapter(sqlQuery, _connectionString))
            {
                logValueAdapter.Fill(lgNew);
            }
            int logCount = _logNames.Count();
            GetIndexValues();
            for (int k = 1; k < logCount; k++)
            {
                string logName = _logNames[k].FixAposInStrings();
                dtNew = LoadLogCurve(logName, k, dtNew);
                if (!_logList.Contains(logName))
                {
                    LASLine mnemInfoItem = _mnemInfo.First(x => x.Mnem == logName);
                    string description = "";
                    if (mnemInfoItem != null)
                    {
                        description = mnemInfoItem.Description;
                    }
                    newRow = lgNew.NewRow();
                    newRow["UWI"] = _uwi;
                    newRow["CURVE_ID"] = logName;
                    newRow["NULL_REPRESENTATION"] = _nullRepresentation;
                    newRow["VALUE_COUNT"] = "-99999.0";
                    newRow["MAX_INDEX"] = "-99999.0";
                    newRow["MIN_INDEX"] = "-99999.0";
                    newRow["SOURCE"] = _logSource;
                    newRow["REPORTED_DESC"] = description;
                    newRow["ROW_CREATED_BY"] = _dbUserName;
                    newRow["ROW_CHANGED_BY"] = _dbUserName;
                    newRow["ROW_CREATED_DATE"] = DateTime.Now.ToString("yyyy-MM-dd");
                    newRow["ROW_CHANGED_DATE"] = DateTime.Now.ToString("yyyy-MM-dd");
                    lgNew.Rows.Add(newRow);
                    _logList.Add(logName);
                }
            }

            using (SqlConnection destinationConnection =
                new SqlConnection(_connectionString))
            {
                destinationConnection.Open();
                using (SqlBulkCopy bulkCopy = new SqlBulkCopy(destinationConnection))
                {
                    bulkCopy.BatchSize = 500;
                    bulkCopy.DestinationTableName = logTable;
                    bulkCopy.WriteToServer(lgNew);
                    bulkCopy.DestinationTableName = logCurvetable;
                    bulkCopy.WriteToServer(dtNew);
                }
            }
        }

        private List<string> GetLogCurveList()
        {
            List<string> curves = new List<string>();

            DataAccessDef dataType = _dataDef.First(x => x.DataType == "LogCurve");
            string tmpUwi = _uwi.FixAposInStrings();
            string query = dataType.Select + $" where UWI = '{tmpUwi}'";
            DataTable dt = _dbAccess.GetDataTable(_connectionString, query);
            curves = dt.AsEnumerable().Select(p => p.Field<string>("CURVE_ID")).Distinct().ToList();

            return curves;
        }

        private List<string> GetLogList()
        {
            List<string> curves = new List<string>();

            DataAccessDef dataType = _dataDef.First(x => x.DataType == "Log");
            string tmpUwi = _uwi.FixAposInStrings();
            string query = dataType.Select + $" where UWI = '{tmpUwi}'";
            DataTable dt = _dbAccess.GetDataTable(_connectionString, query);
            curves = dt.AsEnumerable().Select(p => p.Field<string>("CURVE_ID")).Distinct().ToList();

            return curves;
        }

        private void GetIndexValues()
        {
            int logCount = _logNames.Count();
            int valueCount = _curveValues.Count() / logCount;
            int index = 0;
            for (int m = 0; m < valueCount; m++)
            {
                index = m * logCount;
                _indexValues.Add(_curveValues[index]);
            }
        }

        private DataTable LoadLogCurve(string logName, int pointer,
            DataTable logCurve)
        {
            DataTable newTable = logCurve;
            if (!_logCurveList.Contains(logName))
            {
                newTable = GetNewLogCurve(logCurve, pointer, logName);
                _logCurveList.Add(logName);
            }
            return newTable;
        }

        private DataTable GetNewLogCurve(DataTable dtNew, int pointer, string logName)
        {
            DataTable newTable = dtNew;
            int indexCount = _indexValues.Count();
            int logCount = _logNames.Count();
            DataRow newRow;

            for (int i = 0; i < indexCount; i++)
            {
                newRow = dtNew.NewRow();
                newRow["UWI"] = _uwi;
                newRow["CURVE_ID"] = logName;
                newRow["SAMPLE_ID"] = i;
                newRow["INDEX_VALUE"] = _indexValues[i];
                double measuredValue = _curveValues[pointer + (i * logCount)];
                if (measuredValue < -999999999999999.0 || measuredValue > 999999999999999.0)
                {
                    measuredValue = Convert.ToDouble(_nullRepresentation);
                }
                newRow["MEASURED_VALUE"] = measuredValue;
                newRow["ROW_CREATED_BY"] = _dbUserName;
                newRow["ROW_CHANGED_BY"] = _dbUserName;
                newRow["ROW_CREATED_DATE"] = DateTime.Now.ToString("yyyy-MM-dd");
                newRow["ROW_CHANGED_DATE"] = DateTime.Now.ToString("yyyy-MM-dd");
                dtNew.Rows.Add(newRow);
            }

            return newTable;
        }
    }
}
