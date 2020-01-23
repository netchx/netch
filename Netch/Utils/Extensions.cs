using System.Collections.Generic;
using System.IO;

namespace Netch.Utils
{
    public static class Extensions
    {
        public static IEnumerable<string> GetLines(this string str, bool removeEmptyLines = true)
        {
            using var sr = new StringReader(str);
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                if (removeEmptyLines && string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                yield return line;
            }
        }
    }
}
