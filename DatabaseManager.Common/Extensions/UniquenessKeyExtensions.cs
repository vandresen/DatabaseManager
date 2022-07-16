using DatabaseManager.Shared;
using Microsoft.VisualBasic.FileIO;
using Newtonsoft.Json.Linq;
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
        public static string GetUniqKey(this string jsonData, RuleModel rule)
        {
            string keyText = "";
            string uniqKey = "";
            string[] keyAttributes = rule.RuleParameters.Split(';');
            if (!string.IsNullOrEmpty(jsonData))
            {
                JObject dataObject = JObject.Parse(jsonData);
                foreach (string key in keyAttributes)
                {
                    string function = "";
                    string normalizeParameter = "";
                    string attribute = key.Trim();
                    if (attribute.Substring(0, 1) == "*")
                    {
                        //attribute = attribute.Split('(', ')')[1];
                        int start = attribute.IndexOf("(") + 1;
                        int end = attribute.IndexOf(")", start);
                        function = attribute.Substring(0, start - 1);
                        string csv = attribute.Substring(start, end - start);
                        TextFieldParser parser = new TextFieldParser(new StringReader(csv));
                        parser.HasFieldsEnclosedInQuotes = true;
                        parser.SetDelimiters(",");
                        string[] parms = parser.ReadFields();
                        attribute = parms[0];
                        if (parms.Length > 1) normalizeParameter = parms[1];
                    }
                    string value = dataObject.GetValue(attribute).ToString();
                    if (function == "*NORMALIZE") value = value.NormalizeString(normalizeParameter);
                    if (function == "*NORMALIZE14") value = value.NormalizeString14();
                    keyText = keyText + value;
                    if (!string.IsNullOrEmpty(keyText)) uniqKey = keyText.GetSHA256Hash();
                }
            }
            return uniqKey;
        }
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
