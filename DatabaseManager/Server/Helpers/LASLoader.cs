using DatabaseManager.Server.Entities;
using DatabaseManager.Server.Services;
using DatabaseManager.Shared;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DatabaseManager.Server.Helpers
{
    public class LASLoader
    {
        private DbUtilities _dbConn;
        private string _uwi;
        private string _nullRepresentation;
        private string _logSource;
        private string _dbUserName;
        private List<string> _logCurveList;
        private List<string> _logList;
        private List<string> _logNames = new List<string>();
        private List<LASLine> _mnemInfo;
        private List<double> _curveValues = new List<double>();
        private List<double> _indexValues = new List<double>();
        private List<ReferenceTable> _references = new List<ReferenceTable>();
        private List<DataAccessDef> _dataDef = new List<DataAccessDef>();
        private string connectionString;
        private List<string> LASFiles = new List<string>();
        private readonly IFileStorageService fileStorageService;

        public LASLoader(IFileStorageService fileStorageService)
        {
            _dbConn = new DbUtilities();
            _nullRepresentation = "-999.25";
            _mnemInfo = new List<LASLine>();
            this.fileStorageService = fileStorageService;
        }

        public async Task<List<string>> GetLASFileNames(string catalog)
        {
            List<string> files = new List<string>();
            files = await fileStorageService.ListFiles(catalog);
            return files;
        }

        public async Task<DataTable> GetLASWellHeaders(ConnectParameters source, ConnectParameters target)
        {
            string accessJson = await fileStorageService.ReadFile("connectdefinition", "PPDMDataAccess.json");
            _dataDef = JsonConvert.DeserializeObject<List<DataAccessDef>>(accessJson);
            List<string> files = new List<string>();
            files = await GetLASFileNames(source.Catalog);
            DataAccessDef dataType = _dataDef.First(x => x.DataType == "WellBore");
            string select = dataType.Select;
            string query = $" where 0 = 1";
            _dbConn.OpenConnection(target);
            DataTable dt = _dbConn.GetDataTable(select, query);
            foreach (string file in files)
            {
                string versionInfo = "";
                string wellInfo = "";
                string curveInfo = "";
                string parameterInfo = "";
                string dataInfo = "";
                string fileText = await fileStorageService.ReadFile(source.Catalog, file);
                char[] charSeparators = new char[] { '~' };
                string[] sections = fileText.Split(charSeparators, StringSplitOptions.RemoveEmptyEntries);
                foreach (string section in sections)
                {
                    string flag = section.Substring(0, 1);
                    if (flag == "V") versionInfo = section;
                    if (flag == "W") wellInfo = section;
                    if (flag == "C") curveInfo = section;
                    if (flag == "P") parameterInfo = section;
                    if (flag == "A") dataInfo = section;
                }

                GetVersionInfo(versionInfo);
                //GetCurveInfo(curveInfo);
                string json = GetHeaderInfo(wellInfo);
                DataRow row = dt.NewRow();
                JObject jo = JObject.Parse(json);
                foreach (JProperty property in jo.Properties())
                {
                    string strValue = property.Value.ToString();
                    if (!string.IsNullOrEmpty(strValue))
                    {
                        if (row.Table.Columns.Contains(property.Name))
                        {
                            row[property.Name] = property.Value;
                        }
                    }
                }
                dt.Rows.Add(row);
            }
            return dt;
        }

        public async Task<DataTable> GetLASLogHeaders(ConnectParameters source, ConnectParameters target)
        {
            DataAccessDef dataType = _dataDef.First(x => x.DataType == "Log");
            string select = dataType.Select;
            string query = $" where 0 = 1";
            _dbConn.OpenConnection(target);
            DataTable dt = _dbConn.GetDataTable(select, query);
            List<string> files = new List<string>();
            files = await GetLASFileNames(source.Catalog);
            foreach (string file in files)
            {
                string versionInfo = "";
                string wellInfo = "";
                string curveInfo = "";
                string parameterInfo = "";
                string dataInfo = "";
                string fileText = await fileStorageService.ReadFile(source.Catalog, file);
                char[] charSeparators = new char[] { '~' };
                string[] sections = fileText.Split(charSeparators, StringSplitOptions.RemoveEmptyEntries);
                foreach (string section in sections)
                {
                    string flag = section.Substring(0, 1);
                    if (flag == "V") versionInfo = section;
                    if (flag == "W") wellInfo = section;
                    if (flag == "C") curveInfo = section;
                    if (flag == "P") parameterInfo = section;
                    if (flag == "A") dataInfo = section;
                }

                GetVersionInfo(versionInfo);
                string json = GetHeaderInfo(wellInfo);
                _logNames = new List<string>();
                GetCurveInfo(curveInfo);
                int logCount = _logNames.Count();
                GetIndexValues();
                for (int k = 1; k < logCount; k++)
                {
                    Dictionary<string, string> logHeader = new Dictionary<string, string>();
                    string[] attributes = Common.GetAttributes(dataType.Select);
                    foreach (string attribute in attributes)
                    {
                        logHeader.Add(attribute.Trim(), "");
                    }
                    logHeader["NULL_REPRESENTATION"] = _nullRepresentation;
                    logHeader["VALUE_COUNT"] = "-99999.0";
                    logHeader["MAX_INDEX"] = "-99999.0";
                    logHeader["MIN_INDEX"] = "-99999.0";
                    logHeader["UWI"] = _uwi;
                    logHeader["ROW_CREATED_BY"] = _dbConn.GetUsername();
                    logHeader["ROW_CHANGED_BY"] = _dbConn.GetUsername();
                    string logName = Common.FixAposInStrings(_logNames[k]);
                    logHeader["CURVE_ID"] = logName;
                    json = JsonConvert.SerializeObject(logHeader, Formatting.Indented);
                    DataRow row = dt.NewRow();
                    JObject jo = JObject.Parse(json);
                    foreach (JProperty property in jo.Properties())
                    {
                        string strValue = property.Value.ToString();
                        if (!string.IsNullOrEmpty(strValue))
                        {
                            if (row.Table.Columns.Contains(property.Name))
                            {
                                row[property.Name] = property.Value;
                            }
                        }
                    }
                    dt.Rows.Add(row);
                }
            }

            return dt;
        }

        public async Task LoadLASFile(ConnectParameters source, ConnectParameters target,
            string fileName, string ReferenceTableDefJson)
        {
            _logSource = fileName;
            connectionString = target.ConnectionString;
            _dataDef = JsonConvert.DeserializeObject<List<DataAccessDef>>(target.DataAccessDefinition);
            _references = JsonConvert.DeserializeObject<List<ReferenceTable>>(ReferenceTableDefJson);

            string versionInfo = "";
            string wellInfo = "";
            string curveInfo = "";
            string parameterInfo = "";
            string dataInfo = "";
            string fileText = await fileStorageService.ReadFile(source.Catalog, fileName);
            char[] charSeparators = new char[] { '~' };
            string[] sections = fileText.Split(charSeparators, StringSplitOptions.RemoveEmptyEntries);
            foreach (string section in sections)
            {
                string flag = section.Substring(0, 1);
                if (flag == "V") versionInfo = section;
                if (flag == "W") wellInfo = section;
                if (flag == "C") curveInfo = section;
                if (flag == "P") parameterInfo = section;
                if (flag == "A") dataInfo = section;
            }

            _dbConn.OpenConnection(target);
            _dbUserName = _dbConn.GetUsername();

            GetVersionInfo(versionInfo);
            string json = GetHeaderInfo(wellInfo);
            LoadHeader(json);
            GetCurveInfo(curveInfo);
            GetDataInfo(dataInfo);
            LoadLogs();

            _dbConn.CloseConnection();
        }

        private void LoadLogs()
        {
            DataRow newRow;
            DataTable dtNew = new DataTable();
            DataAccessDef dataType = _dataDef.First(x => x.DataType == "LogCurve");
            string select = dataType.Select;
            string logCurvetable = Common.GetTable(select);
            string sqlQuery = $"select * from {logCurvetable} where 0 = 1";
            SqlDataAdapter logCurveValueAdapter = new SqlDataAdapter(sqlQuery, connectionString);
            logCurveValueAdapter.Fill(dtNew);

            _logCurveList = GetLogCurveList();
            _logList = GetLogList();

            DataTable lgNew = new DataTable();
            dataType = _dataDef.First(x => x.DataType == "Log");
            select = dataType.Select;
            string logTable = Common.GetTable(select);
            sqlQuery = $"select * from {logTable} where 0 = 1";
            SqlDataAdapter logValueAdapter = new SqlDataAdapter(sqlQuery, connectionString);
            logValueAdapter.Fill(lgNew);
            int logCount = _logNames.Count();
            GetIndexValues();
            for (int k = 1; k < logCount; k++)
            {
                string logName = Common.FixAposInStrings(_logNames[k]);
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
                new SqlConnection(connectionString))
            {
                destinationConnection.Open();

                using (SqlBulkCopy bulkCopy =
                new SqlBulkCopy(destinationConnection.ConnectionString))
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
            string select = dataType.Select;
            string query = $" where UWI = '{_uwi}'";
            DataTable dt = _dbConn.GetDataTable(select, query);
            curves = dt.AsEnumerable().Select(p => p.Field<string>("CURVE_ID")).Distinct().ToList();

            return curves;
        }

        private List<string> GetLogList()
        {
            List<string> curves = new List<string>();

            DataAccessDef dataType = _dataDef.First(x => x.DataType == "Log");
            string select = dataType.Select;
            string tmpUwi = Common.FixAposInStrings(_uwi);
            string query = $" where UWI = '{tmpUwi}'";
            DataTable dt = _dbConn.GetDataTable(select, query);
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


        private string GetHeaderInfo(string wellInfo)
        {
            LASHeaderMappings headMap = new LASHeaderMappings();
            DataAccessDef dataType = _dataDef.First(x => x.DataType == "WellBore");
            string input = null;
            Dictionary<string, string> header = new Dictionary<string, string>();
            string[] attributes = Common.GetAttributes(dataType.Select);
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
            while ((input = sr.ReadLine()) != null)
            {
                LASLine line = DecodeLASLine(input);
                if (!string.IsNullOrEmpty(line.Mnem))
                {
                    try
                    {
                        string key = headMap[line.Mnem];
                        header[key] = line.Data;
                        if (key == "NULL") _nullRepresentation = line.Data;
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            if (string.IsNullOrEmpty(header["UWI"])) header["UWI"] = header["API"];
            if (string.IsNullOrEmpty(header["UWI"])) header["UWI"] = header["LEASE_NAME"] + "-" + header["WELL_NAME"];
            string json = JsonConvert.SerializeObject(header, Formatting.Indented);
            _uwi = header["UWI"];
            return json;
        }

        private void LoadHeader(string json)
        {
            List<ReferenceTable> dataTypeRefs = _references.Where(x => x.DataType == "WellBore").ToList();
            foreach (ReferenceTable reference in dataTypeRefs)
            {
                json = CheckHeaderForeignKeys(json, reference);
            }
            DataAccessDef dataType = _dataDef.First(x => x.DataType == "WellBore");
            string tmpUwi = Common.FixAposInStrings(_uwi);
            string select = dataType.Select;
            string query = $" where UWI = '{tmpUwi}'";
            DataTable dt = _dbConn.GetDataTable(select, query);
            if (dt.Rows.Count == 0)
            {
                json = Common.SetJsonDataObjectDate(json, "ROW_CREATED_DATE");
                json = Common.SetJsonDataObjectDate(json, "ROW_CHANGED_DATE");
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
                _dbConn.InsertDataObject(json, "WellBore");
            }
        }

        private string CheckHeaderForeignKeys(string json, ReferenceTable reference)
        {
            try
            {
                JObject dataObject = JObject.Parse(json);
                string field = dataObject[reference.ReferenceAttribute].ToString();
                field = Common.FixAposInStrings(field);
                string select = $"Select * from {reference.Table} ";
                string query = $" where {reference.KeyAttribute} = '{field}'";
                DataTable dt = _dbConn.GetDataTable(select, query);
                if (dt.Rows.Count == 0)
                {
                    if (reference.Insert)
                    {
                        string strInsert = $"insert into {reference.Table} ";
                        string strValue = $" ({reference.KeyAttribute}, {reference.ValueAttribute}) values ('{field}', '{field}')";
                        string strQuery = "";
                        _dbConn.DBInsert(strInsert, strValue, strQuery);
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
    }
}
