using DatabaseManager.Services.Index.Extensions;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DatabaseManager.Services.Index.Services;
using DatabaseManager.Services.Index.Models;

namespace DatabaseManager.Services.Index.Helpers
{
    public class LASLoader
    {
        public class WellMapping
        {
            public string LASMnem { get; set; }
            public string DBMnem { get; set; }
        }

        public class AlernativeKey
        {
            public string Key { get; set; }
        }

        public class LASMappings
        {
            public List<WellMapping> WellMappings { get; set; }
            public List<AlernativeKey> AlernativeKeys { get; set; }
        }

        private readonly IFileStorageService _fileStorageService;
        private List<DataAccessDef> _dataDef = new List<DataAccessDef>();
        private LASMappings lasMappings = new LASMappings();
        private List<LASSections> lasSections = new List<LASSections>();
        private List<string> _logNames = new List<string>();
        private List<double> _curveValues = new List<double>();
        private List<double> _indexValues = new List<double>();
        private List<LASLine> _mnemInfo;
        private string ppdmModel;
        private string _nullRepresentation;
        private string _uwi;

        public LASLoader(IFileStorageService fileStorageService)
        {
            _nullRepresentation = "-999.25";
            _mnemInfo = new List<LASLine>();
            _fileStorageService = fileStorageService;
            //_dp = new DapperDataAccess();
            //_systemData = new SystemDBData(_dp);
        }

        public async Task<DataTable> GetLASWellHeaders(ConnectParametersDto source, ConnectParametersDto target)
        {
            DataTable dt = new DataTable();
            string accessJson = await _fileStorageService.ReadFile("connectdefinition", "PPDMDataAccess.json");
            _dataDef = JsonConvert.DeserializeObject<List<DataAccessDef>>(accessJson);
            string lasMappingsStr = await _fileStorageService.ReadFile("connectdefinition", "LASDataAccess.json");
            lasMappings = JsonConvert.DeserializeObject<LASMappings>(lasMappingsStr);
            ppdmModel = await _fileStorageService.ReadFile("ppdm39", "TAB.sql");

            List<string> files = new List<string>();
            files = await GetLASFileNames(source.Catalog);
            DataAccessDef dataType = _dataDef.First(x => x.DataType == "WellBore");
            dt = NewDataTable("WellBore");
            foreach (string file in files)
            {
                LASSections ls = await GetLASSections(source.Catalog, file);
                lasSections.Add(ls);

                GetVersionInfo(ls.versionInfo);
                string json = await GetHeaderInfo(ls.wellInfo, target.ConnectionString);
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

        public async Task<DataTable> GetLASLogHeaders(ConnectParametersDto source, ConnectParametersDto target)
        {
            DataAccessDef dataType = _dataDef.First(x => x.DataType == "Log");
            DataTable dt = NewDataTable("Log");
            List<string> files = new List<string>();
            files = await GetLASFileNames(source.Catalog);
            foreach (LASSections ls in lasSections)
            {
                GetVersionInfo(ls.versionInfo);
                string json = await GetHeaderInfo(ls.wellInfo, target.ConnectionString);
                _logNames = new List<string>();
                GetCurveInfo(ls.curveInfo);
                int logCount = _logNames.Count();
                GetIndexValues();
                for (int k = 1; k < logCount; k++)
                {
                    Dictionary<string, string> logHeader = new Dictionary<string, string>();
                    string[] attributes = dataType.Select.GetSqlSelectAttributes();
                    foreach (string attribute in attributes)
                    {
                        logHeader.Add(attribute.Trim(), "");
                    }
                    logHeader["NULL_REPRESENTATION"] = _nullRepresentation;
                    logHeader["VALUE_COUNT"] = "-99999.0";
                    logHeader["MAX_INDEX"] = "-99999.0";
                    logHeader["MIN_INDEX"] = "-99999.0";
                    logHeader["UWI"] = _uwi;
                    logHeader["ROW_CREATED_BY"] = "";
                    logHeader["ROW_CHANGED_BY"] = "";
                    string logName = _logNames[k].FixAposInStrings();
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

        public async Task<List<string>> GetLASFileNames(string catalog)
        {
            List<string> files = new List<string>();
            files = await _fileStorageService.ListFiles(catalog);
            return files;
        }

        private async Task<LASSections> GetLASSections(string fileShare, string file)
        {
            LASSections lasSections = new LASSections();
            string fileText = await _fileStorageService.ReadFile(fileShare, file);
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
            string json = "";
            LASHeaderMappings headMap = new LASHeaderMappings();
            DataAccessDef dataType = _dataDef.First(x => x.DataType == "WellBore");
            string input = null;
            Dictionary<string, string> header = new Dictionary<string, string>();
            string[] attributes = dataType.Select.GetSqlSelectAttributes();
            string table = dataType.Select.GetTable();
            List<TableSchema> attributeProperties = Common.GetColumnInfo(table.ToLower(), ppdmModel);
            foreach (string attribute in attributes)
            {
                header.Add(attribute.Trim(), "");
            }
            header["ASSIGNED_FIELD"] = "UNKNOWN";
            header["OPERATOR"] = "UNKNOWN";
            header["DEPTH_DATUM"] = "UNKNOWN";
            header["CURRENT_STATUS"] = "UNKNOWN";
            header["ROW_CREATED_BY"] = "";
            header["ROW_CHANGED_BY"] = "";
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
                WellMapping mapping = lasMappings.WellMappings.FirstOrDefault(s => s.LASMnem == line.Mnem);
                if (mapping != null)
                {
                    string value = mapping.DBMnem;
                    header[value] = line.Data;
                    if (value == "NULL") _nullRepresentation = line.Data;
                }
            }
            foreach (var alternativeKey in lasMappings.AlernativeKeys)
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
                if (dataProperty.DATA_TYPE.ToLower().Contains("varchar"))
                {
                    int? length = Regex.Match(dataProperty.CHARACTER_MAXIMUM_LENGTH, @"\d+").Value.GetIntFromString();
                    if (length != null) header[attribute] = header[attribute].Truncate(length.GetValueOrDefault());
                }
            }
            json = JsonConvert.SerializeObject(header, Formatting.Indented);
            _uwi = header["UWI"];
            return json;
        }

        private DataTable NewDataTable(string dataType)
        {
            DataAccessDef dataAccess = _dataDef.First(x => x.DataType == dataType);
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
    }
}
