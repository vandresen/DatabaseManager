using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public static string NormalizeString14(this string str)
        {
            var charsToRemove = new string[] { "_", "-", "#", "*", ".", "@", "~", " ", "\t", "\n", "\r", "\r\n" };
            foreach (var c in charsToRemove)
            {
                str = str.Replace(c, string.Empty);
            }
            str = str.Replace("&", "AND");
            int length = str.Length;
            if (length < 14)
            {
                char pad = '0';
                str = str.PadRight(14, pad);
            }
            return str;
        }

        public static string NormalizeString(this string str, string parms = "")
        {
            string[] charsToRemove = new string[] { "_", "-", "#", "*", ".", "@", "~", " ", "\t", "\n", "\r", "\r\n" };
            if (!string.IsNullOrEmpty(parms))
            {
                charsToRemove = parms.Select(x => x.ToString()).ToArray();
            }
            
            foreach (var c in charsToRemove)
            {
                str = str.Replace(c, string.Empty);
            }
            str = str.Replace("&", "AND");
            str = str.ToUpper();
            return str;
        }
    }
}
