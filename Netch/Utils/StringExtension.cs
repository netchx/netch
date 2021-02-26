using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Netch.Utils
{
    public static class StringExtension
    {
        public static bool IsNullOrEmpty(this string? value)
        {
            return string.IsNullOrEmpty(value);
        }

        public static bool IsNullOrWhiteSpace(this string? value)
        {
            return string.IsNullOrWhiteSpace(value);
        }

        public static bool BeginWithAny(this string s, IEnumerable<char> chars)
        {
            if (s.IsNullOrEmpty())
                return false;

            return chars.Contains(s[0]);
        }

        public static bool IsWhiteSpace(this string value)
        {
            return value.All(char.IsWhiteSpace);
        }

        public static IEnumerable<string> NonWhiteSpaceLines(this TextReader reader)
        {
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.IsWhiteSpace())
                    continue;

                yield return line;
            }
        }

        public static string ToRegexString(this string value)
        {
            var sb = new StringBuilder();
            foreach (var t in value)
            {
                if (new[] {'\\', '(', ')', '[', ']', '.'}.Any(s => s == t))
                    sb.Append(@"\");

                sb.Append(t);
            }

            return sb.ToString();
        }

        public static string[] SplitRemoveEmptyEntriesAndTrimEntries(this string value, params char[] separator)
        {
            return value.Split(separator).Select(s => s.Trim()).Where(s => s != string.Empty).ToArray();
        }

        public static string[] SplitTrimEntries(this string value, params char[] separator)
        {
            return value.Split(separator).Select(s => s.Trim()).ToArray();
        }

        public static string[] SplitRemoveEmptyEntries(this string value, params char[] separator)
        {
            return value.Split(separator, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}