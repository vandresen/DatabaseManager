using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Common.Entities
{
    public class LithologyInfo
    {
        public enum Lithology
        {
            Unknown,
            Salt,
            Limestone,
            Sandstone,
            Shale
        }

        public Lithology Type { get; set; }
        public double MinValue { get; set; }
        public double MaxValue { get; set; }

        // Centralized definition of all boundaries
        public static readonly List<LithologyInfo> Boundaries = new()
    {
        new LithologyInfo { Type = Lithology.Salt,       MinValue = 0,   MaxValue = 10 },
        new LithologyInfo { Type = Lithology.Limestone,  MinValue = 10,  MaxValue = 20 },
        new LithologyInfo { Type = Lithology.Sandstone,  MinValue = 20,  MaxValue = 55 },
        new LithologyInfo { Type = Lithology.Shale,      MinValue = 55,  MaxValue = 150 }
    };

        public static Lithology GetLithology(double? value)
        {
            if (value == null)
                return Lithology.Unknown;

            foreach (var b in Boundaries)
            {
                if (value >= b.MinValue && value < b.MaxValue)
                    return b.Type;
            }

            return Lithology.Unknown;
        }
    }

}
