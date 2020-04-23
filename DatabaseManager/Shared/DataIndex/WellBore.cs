using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseManager.Shared.DataIndex
{
    public class WellBore
    {
        public int Id { get; set; }
        public string Uwi { get; set; }
        public double Surface_Latitude { get; set; }
        public double Surface_Longitude { get; set; }
        public string Well_Name { get; set; }
        public string Operator { get; set; }
        public string Lease_Name { get; set; }
        public double Final_Td { get; set; }
        public double Depth_Datum_Elev { get; set; }
        public string Depth_Datum { get; set; }
        public string Assigned_Field { get; set; }
        public string Current_Status { get; set; }
        public int ChildrenCount { get; set; }
    }
}
