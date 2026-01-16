using System.Security.Cryptography;
using System.Text;

namespace DatabaseManager.Services.DataQuality.Extensions
{
    public static class UniquenessKeyExtensions
    {
        public static string GetSHA256Hash(this string input)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                byte[] hashBytes = sha256.ComputeHash(inputBytes);
                StringBuilder sb = new StringBuilder();
                foreach (byte b in hashBytes)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
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
