using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseManager.Services.DataQuality.Models
{
    public class DataQcResult
    {
        public bool IsSuccess { get; set; }
        public List<int> FailedIndexes { get; set; } = new();
        public List<string> Messages { get; set; } = new();
    }
}
