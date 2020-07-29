using System;
using System.Collections.Generic;
using System.Linq;

namespace Netch.Models.GitHubRelease
{
    public static class VersionUtil
    {
        public static Release GetLatestRelease(IEnumerable<Release> releases, bool isPreRelease)
        {
            if (!isPreRelease)
            {
                releases = releases.Where(release => !release.prerelease);
            }

            releases = releases.Where(release => IsVersionString(release.tag_name));
            var ordered = releases.OrderByDescending(release => release.tag_name, new VersionComparer());
            return ordered.ElementAt(0);
        }

        private static bool IsVersionString(string str)
        {
            if (Global.Settings.CheckBetaUpdate)
                str = str.Split('-')[0];
            return Version.TryParse(str, out _);
        }

        /// <returns> =0:versions are equal</returns>
        /// <returns> &gt;0:version1 is greater</returns>
        /// <returns> &lt;0:version2 is greater</returns>
        public static int CompareVersion(string v1, string v2)
        {
            var version1 = ToVersion(v1);
            var version2 = ToVersion(v2);
            var res = version1.CompareTo(version2);
            return res;
        }

        private static Version ToVersion(string versionStr)
        {
            var v = versionStr.Split('-');
            var version = Version.Parse(v[0]);
            if (v.Length == 1)
                return version;
            var beta = v[1];

            var result = string.Empty;
            foreach (var c in beta)
            {
                if (int.TryParse(c.ToString(), out var n))
                    result += n;
            }

            return new Version(version.Major, version.Minor, version.Build, int.Parse(result));
        }
    }
}