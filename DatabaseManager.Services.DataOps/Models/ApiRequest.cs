﻿using static DatabaseManager.Services.DataOps.SD;

namespace DatabaseManager.Services.DataOps.Models
{
    public class ApiRequest
    {
        public ApiType ApiType { get; set; } = ApiType.GET;
        public string Url { get; set; }
        public string AzureStorage { get; set; }
        public object Data { get; set; }
    }
}
