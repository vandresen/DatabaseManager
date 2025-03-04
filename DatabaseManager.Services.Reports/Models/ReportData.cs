using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.Reports.Models
{
    public class ReportData
    {
        public int Id { get; set; }
        public string Key { get; set; }
        public string ColumnName { get; set; }
        public string ColumnType { get; set; }
        public string DataType { get; set; }
        public string RuleKey { get; set; }
        public string TextValue { get; set; }
        public double NumberValue { get; set; }
        public string JsonData { get; set; }
        public bool ShowDetails { get; set; }
    }
}
