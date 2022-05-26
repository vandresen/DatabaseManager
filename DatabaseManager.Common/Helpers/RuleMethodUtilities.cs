using DatabaseManager.Shared;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace DatabaseManager.Common.Helpers
{
    public class RuleMethodUtilities
    {
        public class IdwPoint
        {
            public double Distance { get; set; }
            public double Depth { get; set; }
        }

        public class CurveSpikeParameters
        {
            public int WindowSize { get; set; }
            public int SeveritySize { get; set; }
            public double NullValue { get; set; }
        }

        public class StringLengthParameters
        {
            public int Min { get; set; }
            public int Max { get; set; }
        }

        public class ConsistencyParameters
        {
            public string Source { get; set; }
        }

        public class MissingObjectsParameters
        {
            public string DataType { get; set; }
            public List<MissingObjectKey> Keys { get; set; }
            public List<MissingObjectDefault> Defaults { get; set; }
        }

        public class MissingObjectKey
        {
            public string Key { get; set; }
            public string Value { get; set; }
        }

        public class MissingObjectDefault
        {
            public string Default { get; set; }
            public string Value { get; set; }
        }

        public static DataTable GetNeighbors(DbUtilities dbConn, QcRuleSetup qcSetup)
        {
            int indexId = qcSetup.IndexId;
            RuleModel rule = JsonConvert.DeserializeObject<RuleModel>(qcSetup.RuleObject);
            string failRule = rule.FailRule;
            string path = $"$.{rule.DataAttribute}";
            string strProcedure = $"EXEC spGetNeighborsNoFailuresDepth {indexId}, '%{failRule}%', '{path}'";
            DataTable nb = dbConn.GetDataTable(strProcedure, "");
            return nb;
        }

        public static double? CalculateDepthUsingIdw(DataTable nb, QcRuleSetup qcSetup)
        {
            double? depth = null;

            double prevLat = -99999.0;
            double prevLon = -99999.0;
            for (int k = 0; k < nb.Rows.Count; k++)
            {
                double currLat = Common.GetDataRowNumber(nb.Rows[k], "LATITUDE");
                double currLon = Common.GetDataRowNumber(nb.Rows[k], "LONGITUDE");
                double distance = 99999.0;
                if (nb.Columns.Contains("DISTANCE"))
                {
                    distance = Convert.ToDouble(nb.Rows[k]["DISTANCE"]);
                }

                Boolean deleteRow = false;
                if (currLat == -99999.0 && currLon == -99999.0) deleteRow = true;
                if (Math.Abs(prevLat - currLat) < 0.0001 & Math.Abs(prevLon - currLon) < 0.0001) deleteRow = true;
                if (distance < 0.1) deleteRow = true;
                if (deleteRow)
                {
                    nb.Rows[k].Delete();
                }

                prevLat = currLat;
                prevLon = currLon;
            }
            nb.AcceptChanges();

            JObject ruleObject = JObject.Parse(qcSetup.RuleObject);
            string depthAttribute = ruleObject["DataAttribute"].ToString();
            List<IdwPoint> idwPoints = new List<IdwPoint>();

            foreach (DataRow row in nb.Rows)
            {
                double distance = Common.GetDataRowNumber(row, "DISTANCE");
                double number = Common.GetDataRowNumber(row, "DEPTH");
                if (number != -99999.0)
                {
                    idwPoints.Add(new IdwPoint
                    {
                        Distance = distance,
                        Depth = number
                    });
                }
            }

            if (idwPoints.Count > 2) depth = IdwCalculate(idwPoints);

            return depth;
        }

        private static double? IdwCalculate(List<IdwPoint> idwPoints)
        {
            double? depth = 0.0;
            int power = 4;
            double top = 0.0;
            for (int j = 0; j < idwPoints.Count; j++)
            {
                top = top + (idwPoints[j].Depth / Math.Pow(idwPoints[j].Distance, power));
            }

            double bottom = 0.0;
            for (int j = 0; j < idwPoints.Count; j++)
            {
                bottom = bottom + (1 / Math.Pow(idwPoints[j].Distance, power));
            }
            depth = top / bottom;

            return depth;
        }

        public static string ConsistencyCheck(string strValue, string strRefValue, string valueType)
        {
            string status = "Passed";
            if (valueType == "System.Decimal")
            {
                double number;
                double refNumber;
                Boolean isNumber = double.TryParse(strValue, out number);
                if (isNumber)
                {
                    isNumber = double.TryParse(strRefValue, out refNumber);
                    if (isNumber)
                    {
                        if (Math.Abs(refNumber - number) > 0.0000001) status = "Failed";
                    }
                }
            }
            else
            {
                if (strValue != strRefValue) status = "Failed";
            }
            return status;
        }

        public static bool CurveHasSpikes(List<double> curveValues, CurveSpikeParameters spikeParams)
        {
            Boolean spike = false;
            if (curveValues.Count() > 0)
            {
                for (int i = 0; i < curveValues.Count(); i++)
                {
                    if (curveValues[i] != spikeParams.NullValue)
                    {
                        List<double> windowLogValues = GetWindowLogValues(curveValues, i, spikeParams);
                        if (windowLogValues.Count > 2)
                        {

                            spike = GetSpikes(curveValues[i], windowLogValues, spikeParams);
                            if (spike)
                            {
                                break;
                            }
                        }
                    }
                }
            }
            else
            {
                //Console.WriteLine("Error: no log values");
            }
            return spike;
        }

        private static List<double> GetWindowLogValues(List<double> value, int idx, CurveSpikeParameters spikeParams)
        {
            List<double> logValues = new List<double>();
            int start = idx - spikeParams.WindowSize;
            if (start < 0) start = 0;
            int end = idx + spikeParams.WindowSize + 1;
            if (end > value.Count()) end = value.Count();
            for (int i = start; i < end; i++)
            {
                if (value[i] != spikeParams.NullValue) logValues.Add(value[i]);
            }
            return logValues;
        }

        private static Boolean GetSpikes(double value, List<double> windowValues, CurveSpikeParameters spikeParams)
        {
            Boolean spike = false;
            double deviation = Common.CalculateStdDev(windowValues);
            double average = windowValues.Average();
            double spikeFactor = deviation * spikeParams.SeveritySize;
            if (value < average - spikeFactor) spike = true;
            if (value > average + spikeFactor) spike = true;
            return spike;
        }

        public static DataTable GetSortedLogCurve(DataTable lg, string uwi)
        {
            DataTable sortedCurve = new DataTable();

            string query = "UWI = '" + uwi + "'";
            DataRow[] curveRows = lg.Select(query);

            if (curveRows.Length > 0) sortedCurve = curveRows.OrderBy(curve => curve["INDEX_VALUE"]).CopyToDataTable();

            return sortedCurve;
        }

        public static int GetRowNumberForPickDepth(DataTable sortedCurve, double pickDepth)
        {
            int rowNumber = -1;

            for (int j = 0; j < sortedCurve.Rows.Count; j++)
            {
                double value = Convert.ToDouble(sortedCurve.Rows[j]["INDEX_VALUE"]);
                double? nextValue = null;
                if (j < (sortedCurve.Rows.Count - 1))
                    nextValue = Convert.ToDouble(sortedCurve.Rows[j + 1]["INDEX_VALUE"]);
                if (pickDepth >= value && pickDepth < nextValue)
                {
                    rowNumber = j;
                }
            }

            return rowNumber;
        }

        public static double? GetSmoothLogValue(DataTable sortedCurve, double logNullValue, int rowNumber)
        {
            int wndwSize = 25;
            double stdWight = 2.0;
            double? smoothValue = null;

            if (sortedCurve.Rows.Count > 0)
            {
                try
                {
                    double?[] XPointMember = new double?[sortedCurve.Rows.Count];

                    for (int j = 0; j < sortedCurve.Rows.Count; j++)
                    {
                        double measuredValue = Convert.ToDouble(sortedCurve.Rows[j]["MEASURED_VALUE"]);
                        XPointMember[j] = measuredValue;
                        if (measuredValue == logNullValue) XPointMember[j] = null;
                    }

                    HatFunction hatfunction = new HatFunction(XPointMember, wndwSize);
                    double?[] SmoothedValues = hatfunction.SmoothFunction(XPointMember, stdWight);
                    if (rowNumber > -1) smoothValue = SmoothedValues[rowNumber].GetValueOrDefault();
                }
                catch (Exception)
                {
                    Console.WriteLine("Error in getting smoothed curve value");

                }
            }

            return smoothValue;
        }

        public static string GetLogCurveDepths(DbUtilities dbConn, string dataObject)
        {
            JObject logObject = JObject.Parse(dataObject);
            string uwi = logObject["UWI"].ToString();
            string curveName = logObject["CURVE_ID"].ToString();
            string select = "select UWI, INDEX_VALUE, MEASURED_VALUE from WELL_LOG_CURVE_VALUE ";
            string query = $"where CURVE_ID = '{curveName}' and UWI = '{uwi}' order by INDEX_VALUE";
            DataTable lc = dbConn.GetDataTable(select, query);
            //if (lc.Rows.Count == 0) log.Warning("No log curve values are available");

            if (lc.Rows.Count > 0)
            {
                double[] measuredValue = new double[lc.Rows.Count];
                double[] indexValue = new double[lc.Rows.Count]; ;
                for (int j = 0; j < lc.Rows.Count; j++)
                {
                    measuredValue[j] = Convert.ToDouble(lc.Rows[j]["MEASURED_VALUE"]);
                    indexValue[j] = Convert.ToDouble(lc.Rows[j]["INDEX_VALUE"]);
                }

                double topDepth = indexValue.Min();
                logObject["MIN_INDEX"] = topDepth;
                double bottomDepth = indexValue.Max();
                logObject["MAX_INDEX"] = bottomDepth;
                return logObject.ToString();
            }
            else
            {
                return "";
            }
        }

        public static string GetJsonForMissingDataObject(string parameters, DbUtilities dbConn)
        {
            string json = "";
            MissingObjectsParameters missingObjectParms = new MissingObjectsParameters();
            missingObjectParms = JsonConvert.DeserializeObject<MissingObjectsParameters>(parameters);
            string select = "SELECT TOP(1) JSONDATAOBJECT FROM pdo_qc_index";
            string query = $" WHERE DATATYPE = '{missingObjectParms.DataType}'";
            DataTable dt = dbConn.GetDataTable(select, query);
            if (dt.Rows.Count == 1)
            {
                json = dt.Rows[0]["JSONDATAOBJECT"].ToString();
                JObject dataObject = JObject.Parse(json);
                foreach (KeyValuePair<String, JToken> tag in dataObject)
                {
                    var tagName = tag.Key;
                    var variable = tag.Value;
                    var type = variable.Type;
                    if (type == JTokenType.Float)
                    {
                        dataObject[tagName] = -99999.0;
                    }
                    else
                    {
                        dataObject[tagName] = "";
                    }
                }
                json = dataObject.ToString();
            }
            else
            {
                json = "Error";
            }
            return json;
        }

        public static string PopulateJsonForMissingDataObject(string parameters, string emptyJson, string parentData)
        {
            string json = emptyJson;
            try
            {
                MissingObjectsParameters missingObjectParms = new MissingObjectsParameters();
                missingObjectParms = JsonConvert.DeserializeObject<MissingObjectsParameters>(parameters);
                JObject dataObject = JObject.Parse(emptyJson);
                JObject parentObject = JObject.Parse(parentData);
                foreach (var key in missingObjectParms.Keys)
                {
                    var tagName = key.Key;
                    var variable = key.Value;
                    if (variable.Substring(0, 1) == "!")
                    {
                        string parentTag = key.Value.Substring(1);
                        variable = parentObject[parentTag].ToString();
                    }
                    dataObject[tagName] = variable;
                }
                dataObject["REMARK"] = $"Has been predicted and created by QCEngine;";
                json = dataObject.ToString();
            }
            catch (Exception ex)
            {
                json = "Error";
            }

            return json;
        }

        public static string AddDefaultsForMissingDataObjects(string parameters, string inputJson)
        {
            string json = inputJson;
            try
            {
                MissingObjectsParameters missingObjectParms = new MissingObjectsParameters();
                missingObjectParms = JsonConvert.DeserializeObject<MissingObjectsParameters>(parameters);
                if (missingObjectParms.Defaults != null)
                {
                    JObject dataObject = JObject.Parse(inputJson);
                    foreach (var key in missingObjectParms.Defaults)
                    {
                        var tagName = key.Default;
                        var variable = key.Value;
                        dataObject[tagName] = variable;
                    }
                    json = dataObject.ToString();
                }
            }
            catch (Exception ex)
            {
                json = "Error";
            }

            return json;
        }
    }
}
