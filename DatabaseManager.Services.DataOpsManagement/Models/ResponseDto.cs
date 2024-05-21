using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.DataOpsManagement.Models
{
    public class ResponseDto
    {
        public bool IsSuccess { get; set; } = true;
        public object Result { get; set; }
        public string DisplayMessage { get; set; } = "";
        public List<string> ErrorMessages { get; set; }
    }
}
