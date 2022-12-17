using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.BlazorComponents.Models
{
    public class SurfacePoint
    {
        public double Value { get; }

        public double[] Coordinates { get; }

        public SurfacePoint(double value, params double[] coordinates)
        {
            Value = value;
            Coordinates = coordinates;
        }

        public override string ToString()
        {
            return $"{string.Join(";", Coordinates)} -> {Value}";
        }
    }
}
