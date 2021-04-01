using System;
using System.Collections.Generic;
using System.Linq;

namespace Netch.Models.GitHubRelease
{
    public static class VersionUtil
    {
        private static VersionComparer instance = new();

        public static int CompareVersion(string x, string y)
        {
            return instance.Compare(x, y);
        }

        public class VersionComparer : IComparer<string>
        {
            /// <summary>
            ///     Greater than 0 newer
            /// </summary>
            /// <param name="x"></param>
            /// <param name="y"></param>
            /// <returns></returns>
            public int Compare(string? x, string? y)
            {
                var xResult = SuffixVersion.TryParse(x, out var version1) ? 1 : 0;
                var yResult = SuffixVersion.TryParse(y, out var version2) ? 1 : 0;

                var parseResult = xResult - yResult;
                if (parseResult != 0)
                    return parseResult;

                return version1.CompareTo(version2);
            }
        }
    }
}