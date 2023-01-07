using System.Security.Cryptography;
using System.Text;

namespace EDUtils
{
    public static class HashUtil
    {
        private static string ToHex(byte[] bytes, bool upperCase)
        {
            StringBuilder result = new(bytes.Length * 2);
            for (int i = 0; i < bytes.Length; i++)
            {
                result.Append(bytes[i].ToString(upperCase ? "X2" : "x2"));
            }
            return result.ToString();
        }

        public static string SHA256Hex(string input)
        {
            byte[] hash = SHA256.HashData(Encoding.Default.GetBytes(input));
            string hashString = ToHex(hash, false);
            return hashString;
        }
    }
}
