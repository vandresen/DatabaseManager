using DatabaseManager.Shared;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace DatabaseManager.Server.Helpers
{
    public class RuleMethodUtilities
    {
        public class IdwPoint
        {
            public double Distance { get; set; }
            public double Depth { get; set; }
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
    }
}
