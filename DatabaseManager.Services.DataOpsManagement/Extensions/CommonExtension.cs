﻿using Microsoft.Azure.Functions.Worker.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.DataOpsManagement.Extensions
{
    public static class CommonExtension
    {
        public static string GetStorageKey(this HttpRequestData req)
        {
            var headers = req.Headers;
            IEnumerable<string> headerSerachResult;
            string storageAccount = string.Empty;
            if (headers.TryGetValues("azurestorageconnection", out headerSerachResult))
            {
                storageAccount = headerSerachResult.First();
            }
            if (string.IsNullOrEmpty(storageAccount))
            {
                Exception error = new Exception($"Error getting azure storage key");
                throw error;
            }
            return storageAccount;
        }
    }
}
