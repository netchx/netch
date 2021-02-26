using System.Collections.Generic;

namespace Netch.Models.GitHubRelease
{
    public class VersionComparer : IComparer<object>
    {
        public int Compare(object x, object y)
        {
            return VersionUtil.CompareVersion(x.ToString(), y.ToString());
        }
    }
}