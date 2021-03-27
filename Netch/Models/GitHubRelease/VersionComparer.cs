using System;
using System.Collections.Generic;

namespace Netch.Models.GitHubRelease
{
    public class VersionComparer : IComparer<object>
    {
        public int Compare(object? x, object? y)
        {
            if (x == null)
                throw new ArgumentNullException(nameof(x));

            if (y == null)
                throw new ArgumentNullException(nameof(y));

            return VersionUtil.CompareVersion(x.ToString(), y.ToString());
        }
    }
}