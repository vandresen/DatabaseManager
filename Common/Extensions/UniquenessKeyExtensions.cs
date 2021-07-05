using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace DatabaseManager.Common.Extensions
{
    public static class UniquenessKeyExtensions
    {
        public static string GetSHA256Hash(this string str)
        {
            StreamWriter sw = null;
            try
            {
                UnicodeEncoding uniEncoding = new UnicodeEncoding();
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    sw = new StreamWriter(memoryStream, uniEncoding);
                    sw.Write(str);
                    sw.Flush();
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    SHA256CryptoServiceProvider provider = new SHA256CryptoServiceProvider();
                    provider.ComputeHash(memoryStream.ToArray());
                    string strHex = BitConverter.ToString(provider.Hash);
                    strHex = strHex.Replace("-", "");
                    return (strHex);
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
