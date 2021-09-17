using AutoMapper;
using DatabaseManager.Common.Entities;
using DatabaseManager.Common.Services;
using DatabaseManager.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Common.Helpers
{
    public class Common
    {
        public static string CreateDatabaseConnectionString(ConnectParameters connection)
        {
            string source = $"Source={connection.DatabaseServer};";
            string database = $"Initial Catalog ={connection.Catalog};";
            string timeout = "Connection Timeout=120";
            string persistSecurity = "Persist Security Info=False;";
            string multipleActive = "MultipleActiveResultSets=True;";
            string integratedSecurity = "";
            string user = "";
            //Encryption is currently not used, more testing later
            //string encryption = "Encrypt=True;TrustServerCertificate=False;";
            if (!string.IsNullOrWhiteSpace(connection.User))
                user = $"User ID={connection.User};";
            else
                integratedSecurity = "Integrated Security=True;";
            string password = "";
            if (!string.IsNullOrWhiteSpace(connection.Password)) password = $"Password={connection.Password};";

            string cnStr = "Data " + source + persistSecurity + database +
                user + password + integratedSecurity + multipleActive;

            cnStr = cnStr + timeout;

            return cnStr;
        }

        public static async Task<ConnectParameters> GetConnectParameters(string azureConnectionString, string connecterSource)
        {
            if (String.IsNullOrEmpty(azureConnectionString))
            {
                Exception error = new Exception($"Azure Connection string is not set");
                throw error;
            }
            var builder = new ConfigurationBuilder();
            IConfiguration configuration = builder.Build();
            ITableStorageServiceCommon tableStorage = new AzureTableStorageServiceCommon(configuration);
            tableStorage.SetConnectionString(azureConnectionString);

            ConnectParameters connector = new ConnectParameters();
            SourceEntity entity = await tableStorage.GetTableRecord<SourceEntity>("sources", connecterSource);
            if (entity == null)
            {
                Exception error = new Exception($"Could not find source connector");
                throw error;
            }
            var config = new MapperConfiguration(cfg => {
                cfg.CreateMap<SourceEntity, ConnectParameters>().ForMember(dest => dest.SourceName, opt => opt.MapFrom(src => src.RowKey));
            });
            IMapper mapper = config.CreateMapper();
            connector = mapper.Map<ConnectParameters>(entity);

            return connector;
        }

        public static string ConvertDataRowToJson(DataRow dataRow, DataTable dt)
        {
            DataTable tmp = new DataTable();
            tmp = dt.Clone();
            tmp.Rows.Add(dataRow.ItemArray);
            string jsonData = JsonConvert.SerializeObject(tmp);
            jsonData = jsonData.Replace("[", "");
            jsonData = jsonData.Replace("]", "");
            return jsonData;
        }

        public static string FixAposInStrings(string st)
        {
            string fixString = st;
            int length;
            int start = 0;
            int end = st.IndexOf("'");
            while (end >= 0)
            {
                length = end;
                string s1 = fixString.Substring(0, length);
                string s2 = fixString.Substring(end);
                fixString = s1 + "'" + s2;
                start = end + 2;
                end = fixString.IndexOf("'", start);
            }
            return fixString;
        }

        public static string[] GetAttributes(string select)
        {
            int from = 7;
            int to = select.IndexOf("from");
            int length = to - 8;
            string attributes = select.Substring(from, length);
            string[] words = attributes.Split(',');

            return words;
        }

        public static string GetTable(string select)
        {
            select = select.ToUpper();
            int from = select.IndexOf(" FROM ") + 6;
            string table = select.Substring(from);
            return table;
        }

        public static string SetJsonDataObjectDate(string jsonText, string attribute)
        {
            string jsonDate = DateTime.Now.ToString("yyyy-MM-dd");
            JObject dataObject = JObject.Parse(jsonText);
            dataObject[attribute] = jsonDate;
            jsonText = dataObject.ToString();
            return jsonText;
        }

        public static bool Between(double x, double min, double max)
        {
            return (min < x) && (x < max);
        }

        public static double GetDataRowNumber(DataRow dr, string attribute)
        {
            double number = -99999.0;
            if (!string.IsNullOrEmpty(attribute))
            {
                string strNumber = dr[attribute].ToString();
                if (!string.IsNullOrEmpty(strNumber))
                {
                    Boolean isNumber = double.TryParse(strNumber, out number);
                    if (!isNumber) number = -99999.0;
                }
            }
            return number;
        }

        public static double GetLogNullValue(string jsonData)
        {
            double nullValue = -999.2500;
            JObject json = JObject.Parse(jsonData);
            JToken jsonToken = json["NULL_REPRESENTATION"];
            if (jsonToken is null)
            {
                Console.WriteLine("Error: NULL value is null");
            }
            else
            {
                if (double.TryParse(jsonToken.ToString(), out double value))
                {
                    nullValue = value;
                }
                else
                {
                    Console.WriteLine("Error: Not a proper null number");
                }

            }
            return nullValue;
        }

        public static string CompletenessCheck(string strValue)
        {
            string status = "Passed";
            if (string.IsNullOrWhiteSpace(strValue))
            {
                status = "Failed";
            }
            else
            {
                double number;
                bool canConvert = double.TryParse(strValue, out number);
                if (canConvert)
                {
                    if (number == -99999) status = "Failed";
                }
            }
            return status;
        }

        public static double CalculateStdDev(List<double> values)
        {
            double stdDev = 0;
            if (values.Count > 2)
            {
                double average = values.Average();
                double sum = values.Sum(d => Math.Pow(d - average, 2));
                stdDev = Math.Sqrt((sum) / (values.Count() - 1));
            }
            return stdDev;
        }

        public static string GetCreateSQLFromDataTable(string tableName, DataTable schema)
        {
            string sql = "CREATE TABLE [" + tableName + "] (\n";

            // columns
            foreach (DataColumn column in schema.Columns)
            {
                string columnName = column.ColumnName;
                string columnType = DataTableToSQLConvertType(column);
                sql += "\t[" + columnName + "] " + columnType;
                sql += ",\n";
            }
            sql = sql.TrimEnd(new char[] { ',', '\n' }) + "\n";

            sql += ")";

            return sql;
        }

        public static string DataTableToSQLConvertType(DataColumn column)
        {
            string type = column.DataType.Name;
            int columnSize = column.MaxLength;
            switch (type)
            {
                case "String":
                    return "VARCHAR(" + ((columnSize == -1) ? "255" : (columnSize > 8000) ? "MAX" : columnSize.ToString()) + ")";

                case "Decimal":
                    return "REAL";

                case "Double":
                case "Single":
                    return "REAL";

                case "Int64":
                    return "BIGINT";

                case "Int16":
                case "Int32":
                    return "INT";

                case "DateTime":
                    return "DATETIME";

                case "Boolean":
                    return "BIT";

                case "Byte":
                    return "TINYINT";

                default:
                    throw new Exception(type.ToString() + " not implemented.");
            }
        }

        public static RuleModel GetRule(DbUtilities dbConn, int id, List<DataAccessDef> accessDefs)
        {
            List<RuleModel> rules = new List<RuleModel>();
            DataAccessDef ruleAccessDef = accessDefs.First(x => x.DataType == "Rules");
            string sql = ruleAccessDef.Select;
            string query = $" where Id = {id}";
            DataTable dt = dbConn.GetDataTable(sql, query);
            string jsonString = JsonConvert.SerializeObject(dt);
            rules = JsonConvert.DeserializeObject<List<RuleModel>>(jsonString);
            RuleModel rule = rules.First();

            DataAccessDef functionAccessDef = accessDefs.First(x => x.DataType == "Functions");
            sql = functionAccessDef.Select;
            query = $" where FunctionName = '{rule.RuleFunction}'";
            dt = dbConn.GetDataTable(sql, query);

            string functionURL = dt.Rows[0]["FunctionUrl"].ToString();
            string functionKey = dt.Rows[0]["FunctionKey"].ToString();
            if (!string.IsNullOrEmpty(functionKey)) functionKey = "?code=" + functionKey;
            rule.RuleFunction = functionURL + functionKey;
            return rule;
        }

        public static string GetStorageKey(HttpRequest req, ILogger log)
        {
            var headers = req.Headers;
            string storageAccount = headers.FirstOrDefault(x => x.Key == "azurestorageconnection").Value;
            if (string.IsNullOrEmpty(storageAccount))
            {
                Exception error = new Exception($"Error getting azure storage key");
                throw error;
            }
            return storageAccount;
        }
    }
}
