﻿using DatabaseManager.Common.Data;
using DatabaseManager.Common.DBAccess;
using DatabaseManager.Common.Entities;
using DatabaseManager.Shared;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data;

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

        public class IsEqualToParameters
        {
            public string Value { get; set; }
            public string Delimiter { get; set; }
        }

        public class IsGreaterThanParameters
        {
            public double Value { get; set; }
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

        public static double? CalculateDepthUsingIdw(IEnumerable<NeighbourIndex> nb, QcRuleSetup qcSetup)
        {
            double? depth = null;

            double prevLat = -99999.0;
            double prevLon = -99999.0;

            List<NeighbourIndex> validNbs = new List<NeighbourIndex>();
            foreach(NeighbourIndex neighbour in nb)
            {
                Boolean deleteRow = false;
                if (neighbour.Latitude == -99999.0 && neighbour.Longitude == -99999.0) deleteRow = true;
                if (Math.Abs(prevLat - neighbour.Latitude) < 0.0001 & Math.Abs(prevLon - neighbour.Longitude) < 0.0001) deleteRow = true;
                if (neighbour.Distance < 0.1) deleteRow = true;
                if (!deleteRow)
                {
                    validNbs.Add(neighbour);
                }
                prevLat = neighbour.Latitude;
                prevLon = neighbour.Longitude;
            }

            JObject ruleObject = JObject.Parse(qcSetup.RuleObject);
            string depthAttribute = ruleObject["DataAttribute"].ToString();
            List<IdwPoint> idwPoints = new List<IdwPoint>();

            foreach(NeighbourIndex nbi in validNbs)
            {
                if (nbi.Depth != -99999.0)
                {
                    idwPoints.Add(new IdwPoint
                    {
                        Distance = nbi.Distance,
                        Depth = nbi.Depth
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

        public static string GetLogCurveDepths(string dataObject)
        {
            JObject logObject = JObject.Parse(dataObject);
            string uwi = logObject["UWI"].ToString();
            string curveName = logObject["CURVE_ID"].ToString();
            string indexValues = logObject["INDEX_VALUE"].ToString();
            if (string.IsNullOrEmpty(indexValues))
            {
                return "";
            }
            else
            {
                double[] indexValue = Common.GetArrayFromString<double>(indexValues);
                double topDepth = indexValue.Min();
                logObject["MIN_INDEX"] = topDepth;
                double bottomDepth = indexValue.Max();
                logObject["MAX_INDEX"] = bottomDepth;
                return logObject.ToString();
            }
        }

        public static string GetJsonForMissingDataObject(string parameters, DataAccessDef accessDef,
            IEnumerable<TableSchema> attributeProperties)
        {
            string json = "{}";
            MissingObjectsParameters missingObjectParms = new MissingObjectsParameters();
            missingObjectParms = JsonConvert.DeserializeObject<MissingObjectsParameters>(parameters);

            string[] columns = Common.GetAttributes(accessDef.Select);
            JObject dataObject = JObject.Parse(json);
            foreach (string column in columns)
            {
                    TableSchema tableSchema = attributeProperties.FirstOrDefault(x => x.COLUMN_NAME == column.Trim());
                    if (tableSchema == null)
                    {
                        break;
                    }
                    else
                    {
                        string type = tableSchema.TYPE_NAME.ToLower();
                        if (type == "numeric")
                        {
                            dataObject[column.Trim()] = -99999.0;
                        }
                        else if(type == "datetime")
                        {
                            dataObject[column.Trim()] = DateTime.Now.ToString("yyyy-MM-dd");
                        }
                        else
                        {
                            dataObject[column.Trim()] = "";
                        }
                    }
            }
            json = dataObject.ToString();

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

        public static DataAccessDef GetDataAccessDefintionFromRoot(IndexDBAccess idxdata, string dataConnector, string dataType)
        {
            IndexModel idxResult = Task.Run(() => idxdata.GetIndexRoot(dataConnector)).GetAwaiter().GetResult();
            string jsonStringObject = idxResult.JsonDataObject;
            IndexRootJson rootJson = JsonConvert.DeserializeObject<IndexRootJson>(jsonStringObject);
            ConnectParameters source = JsonConvert.DeserializeObject<ConnectParameters>(rootJson.Source);
            List<DataAccessDef> accessDefs = JsonConvert.DeserializeObject<List<DataAccessDef>>(source.DataAccessDefinition);
            DataAccessDef accessDef = accessDefs.First(x => x.DataType == dataType);
            return accessDef;
        }

        public static IndexRootJson GetIndexRoot(IndexDBAccess idxdata, string dataConnector)
        {
            IndexModel idxResult = Task.Run(() => idxdata.GetIndexRoot(dataConnector)).GetAwaiter().GetResult();
            string jsonStringObject = idxResult.JsonDataObject;
            IndexRootJson rootJson = JsonConvert.DeserializeObject<IndexRootJson>(jsonStringObject);
            return rootJson;
        }
    }
}
