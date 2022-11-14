using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Common.Entities
{
    public class TableSchema
    {
        public string TABLE_NAME { get; set; }
        public string COLUMN_NAME { get; set; }
        public string DATA_TYPE { get; set; }
        public string TYPE_NAME { get; set; }
        public string CHARACTER_MAXIMUM_LENGTH { get; set; }
        public string NUMERIC_PRECISION { get; set; }
        public string NUMERIC_SCALE { get; set; }
        public string PRECISION { get; set; }
        public string SCALE { get; set; }
    }
}
