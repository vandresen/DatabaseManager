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
        private List<string> _logNames = new List<string>();
        private List<double> _curveValues = new List<double>();
        private List<double> _indexValues = new List<double>();
        private List<ReferenceTable> _references = new List<ReferenceTable>();
        private List<DataAccessDef> _dataDef = new List<DataAccessDef>();
        private string connectionString;
        private readonly IFileStorageService fileStorageService;
        private readonly ITableStorageService tableStorageService;

        public LASLoader(IFileStorageService fileStorageService,
            ITableStorageService tableStorageService)
        {
            _dbConn = new DbUtilities();
            _nullRepresentation = "-999.25";
            this.fileStorageService = fileStorageService;
            this.tableStorageService = tableStorageService;
        }

        public async Task LoadLASFile(ConnectParameters source, ConnectParameters target, string fileName)
        {
            connectionString = target.ConnectionString;
            string accessJson = await fileStorageService.ReadFile("connectdefinition", "PPDMDataAccess.json");
            _dataDef = JsonConvert.DeserializeObject<List<DataAccessDef>>(accessJson);
            string referenceJson = await fileStorageService.ReadFile("connectdefinition", "PPDMReferenceTables.json");
            _references = JsonConvert.DeserializeObject<List<ReferenceTable>>(referenceJson);

            string versionInfo = "";
            string wellInfo = "";
            string curveInfo = "";
            string parameterInfo = "";
            string dataInfo = "";
            string fileText = await fileStorageService.ReadFile(source.Catalog, fileName);
            string[] sections = fileText.Split("~", StringSplitOptions.RemoveEmptyEntries);
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

            GetVersionInfo(versionInfo);
            GetHeaderInfo(wellInfo);
            GetCurveInfo(curveInfo);
            GetDataInfo(dataInfo);
            LoadLogs();

            _dbConn.CloseConnection();
        }

        private void LoadLogs()
        {
            DataAccessDef dataType = _dataDef.First(x => x.DataType == "Log");
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
                string json = JsonConvert.SerializeObject(logHeader, Formatting.Indented);
                LoadLogHeader(json, logName, k);
            }
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

        private void LoadLogHeader(string json, string logName, int pointer)
        {
            DataAccessDef dataType = _dataDef.First(x => x.DataType == "Log");
            string select = dataType.Select;
            string query = $" where UWI = '{_uwi}' and CURVE_ID = '{logName}'";
            DataTable dt = _dbConn.GetDataTable(select, query);
            if (dt.Rows.Count == 0)
            {
                json = Common.SetJsonDataObjectDate(json, "ROW_CREATED_DATE");
                json = Common.SetJsonDataObjectDate(json, "ROW_CHANGED_DATE");
                
                _dbConn.InsertDataObject(json, "Log");
                LoadLogCurve(pointer, logName);
            }
        }

        private void LoadLogCurve(int pointer, string logName)
        {
            DataAccessDef dataType = _dataDef.First(x => x.DataType == "LogCurve");
            string select = dataType.Select;
            string query = $" where UWI = '{_uwi}' and CURVE_ID = '{logName}'";
            DataTable dt = _dbConn.GetDataTable(select, query);

            if (dt.Rows.Count == 0)
            {
                int indexCount = _indexValues.Count();
                int logCount = _logNames.Count();

                DataRow newRow;
                DataTable dtNew = new DataTable();
                string sqlQuery = dataType.Select + " where 0 = 1";
                SqlDataAdapter logCurveValueAdapter = new SqlDataAdapter(sqlQuery, connectionString);
                logCurveValueAdapter.Fill(dtNew);

                for (int i = 0; i < indexCount; i++)
                {
                    newRow = dtNew.NewRow();
                    newRow["UWI"] = _uwi;
                    newRow["CURVE_ID"] = logName;
                    newRow["SAMPLE_ID"] = i;
                    newRow["INDEX_VALUE"] = _indexValues[i];
                    newRow["MEASURED_VALUE"] = _curveValues[pointer + (i * logCount)];
                    newRow["ROW_CREATED_BY"] = _dbConn.GetUsername();
                    newRow["ROW_CHANGED_BY"] = _dbConn.GetUsername();
                    newRow["ROW_CREATED_DATE"] = DateTime.Now.ToString("yyyy-MM-dd");
                    newRow["ROW_CHANGED_DATE"] = DateTime.Now.ToString("yyyy-MM-dd");
                    dtNew.Rows.Add(newRow);
                }
                new SqlCommandBuilder(logCurveValueAdapter);
                logCurveValueAdapter.Update(dtNew);
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

        private void GetCurveInfo(string curveInfo)
        {
            DataAccessDef dataType = _dataDef.First(x => x.DataType == "Log");
            Dictionary<string, string> log = new Dictionary<string, string>();
            string[] attributes = Common.GetAttributes(dataType.Select);
            foreach (string attribute in attributes)
            {
                log.Add(attribute.Trim(), "");
            }

            string input = null;
            StringReader sr = new StringReader(curveInfo);
            while ((input = sr.ReadLine()) != null)
            {
                LASLine line = DecodeLASLine(input);
                if (!string.IsNullOrEmpty(line.Mnem))
                {
                    log["CURVE_ID"] = line.Mnem.Trim();
                    _logNames.Add(line.Mnem.Trim());
                    log["UWI"] = _uwi;
                    var json = JsonConvert.SerializeObject(log, Formatting.Indented);
                }
            }
        }

        
        private void GetHeaderInfo(string wellInfo)
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
            header["ROW_CREATED_BY"] = _dbConn.GetUsername();
            header["ROW_CHANGED_BY"] = _dbConn.GetUsername();
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
            LoadHeader(json);
        }

        private void LoadHeader(string json)
        {
            List<ReferenceTable> dataTypeRefs = _references.Where(x => x.DataType == "WellBore").ToList();
            foreach (ReferenceTable reference in dataTypeRefs)
            {
                json = CheckHeaderForeignKeys(json, reference);
            }
            DataAccessDef dataType = _dataDef.First(x => x.DataType == "WellBore");
            string select = dataType.Select;
            string query = $" where UWI = '{_uwi}'";
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
                    if (line.Data.Substring(0,3) != "2.0")
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
                    string unit = string.Empty;
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
