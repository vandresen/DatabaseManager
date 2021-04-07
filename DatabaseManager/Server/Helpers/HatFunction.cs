using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DatabaseManager.Server.Helpers
{
    public class IntervalWindow
    {
        public IntervalWindow()
        {
        }

        public int StartIndex { get; set; }
        public int EndIndex { get; set; }
    }

    public class HatWindow
    {
        public HatWindow()
        {
            TrailingAverage = null;
            TrailingSTD = null;
            LeadingAverage = null;
            LeadingSTD = null;
            WindowAverage = null;
            WindowSTD = null;


        }
        public double? TrailingAverage { get; set; }
        public double? TrailingSTD { get; set; }
        public double? LeadingAverage { get; set; }
        public double? LeadingSTD { get; set; }
        public double? WindowAverage { get; set; }
        public double? WindowSTD { get; set; }
        public double? Value { get; set; }


    }

    public class HatFunction
    {
        public HatWindow[] HatWindows;
        public HatFunction(double?[] values, int iWindowsSize = 25)
        {
            HatWindows = new HatWindow[values.Length];
            for (int i = 0; i < values.Length; ++i)
            {
                HatWindows[i] = new HatWindow();
            }
            for (Int32 i = 0; i < values.Length; ++i)
            {
                double?[] BackWindow = SubSet(i - iWindowsSize, i, values);
                double?[] MiddleWindow = SubSet(i - iWindowsSize / 2, i + iWindowsSize / 2, values);
                double?[] ForwardWindow = SubSet(i, i + iWindowsSize, values);
                HatWindows[i].Value = values[i];
                if (BackWindow != null)
                {
                    HatWindows[i].TrailingAverage = MathNet.Numerics.Statistics.Statistics.Mean(BackWindow);
                    HatWindows[i].TrailingSTD = MathNet.Numerics.Statistics.Statistics.StandardDeviation(BackWindow);
                }
                else
                {
                    HatWindows[i].TrailingAverage = null;
                    HatWindows[i].TrailingSTD = null;
                }

                if (MiddleWindow != null)
                {
                    HatWindows[i].WindowAverage = MathNet.Numerics.Statistics.Statistics.Mean(MiddleWindow);
                    HatWindows[i].WindowSTD = MathNet.Numerics.Statistics.Statistics.StandardDeviation(MiddleWindow);
                }
                else
                {
                    HatWindows[i].WindowAverage = null;
                    HatWindows[i].WindowSTD = null;
                }


                if (ForwardWindow != null)
                {
                    HatWindows[i].LeadingAverage = MathNet.Numerics.Statistics.Statistics.Mean(ForwardWindow);
                    HatWindows[i].LeadingSTD = MathNet.Numerics.Statistics.Statistics.StandardDeviation(ForwardWindow);
                }
                else
                {
                    HatWindows[i].LeadingAverage = null;
                    HatWindows[i].LeadingAverage = null;
                }
            }

        }

        public double?[] SmoothFunction(double?[] values, double stdWeight = 2.0)
        {
            double?[] SmoothedValues = new double?[values.Length];
            double?[] RatioValues = new double?[values.Length];
            for (int i = 0; i < values.Length; ++i)
            {
                HatWindow window = HatWindows[i];
                if (window.LeadingSTD.HasValue && window.WindowSTD.HasValue)
                {
                    RatioValues[i] = window.LeadingSTD / window.WindowSTD;
                    if (double.IsNaN(RatioValues[i].Value))
                    {
                        RatioValues[i] = null;
                    }
                }
                else
                {
                    RatioValues[i] = null;
                }
            }
            double? MaxRatio = MathNet.Numerics.Statistics.Statistics.Maximum(RatioValues);
            double? MinRatio = MathNet.Numerics.Statistics.Statistics.Minimum(RatioValues);
            double? AvgRatio = MathNet.Numerics.Statistics.Statistics.Mean(RatioValues);
            double? StdRatio = MathNet.Numerics.Statistics.Statistics.StandardDeviation(RatioValues);

            double WindowCutoffValue = AvgRatio.Value + stdWeight * StdRatio.Value;

            List<IntervalWindow> intervalWindows = new List<IntervalWindow>();
            IntervalWindow currentWindow = new IntervalWindow();
            intervalWindows.Add(currentWindow);
            currentWindow.StartIndex = 0;
            for (int i = 0; i < values.Length; ++i)
            {
                if (RatioValues[i].HasValue)
                {
                    if (RatioValues[i].Value > WindowCutoffValue)
                    {
                        currentWindow.EndIndex = i;
                        currentWindow = new IntervalWindow();
                        intervalWindows.Add(currentWindow);
                        currentWindow.StartIndex = i;
                    }
                }
            }
            currentWindow.EndIndex = values.Length - 1;

            foreach (IntervalWindow window in intervalWindows)
            {
                double?[] WindowValues = SubSet(window.StartIndex, window.EndIndex, values);
                double? average = MathNet.Numerics.Statistics.Statistics.Mean(WindowValues);
                for (int i = window.StartIndex; i < window.EndIndex; ++i)
                {
                    SmoothedValues[i] = average;
                }
            }
            return (SmoothedValues);
        }
        double?[] SubSet(int iStart, int iEnd, double?[] values)
        {
            iStart = Math.Max(0, iStart);
            iEnd = Math.Min(iEnd, values.Length - 1);
            int iLength = (iEnd - iStart);
            if (iLength == 0)
            {
                return (null);
            }
            double?[] result = new double?[iLength];
            Array.Copy(values, iStart, result, 0, iLength);
            return result;
        }
    }
}
