using System;
using System.Text;

namespace Netch.Utils
{
    public static class Base64
    {
        public static class Encode
        {
            public static string Normal(string text)
            {
                return Convert.ToBase64String(Encoding.UTF8.GetBytes(text));
            }

            public static string URLSafe(string text)
            {
                return Convert.ToBase64String(Encoding.UTF8.GetBytes(text)).Replace("+", "-").Replace("/", "_").Replace("=", "");
            }
        }

        public static class Decode
        {
            public static string Normal(string text)
            {
                return Encoding.UTF8.GetString(Convert.FromBase64String(text.PadRight(text.Length + (4 - text.Length % 4) % 4, '=')));
            }

            public static string URLSafe(string text)
            {
                return Encoding.UTF8.GetString(Convert.FromBase64String(text.Replace("-", "+").Replace("_", "/").PadRight(text.Length + (4 - text.Length % 4) % 4, '=')));
            }
        }
    }
}
