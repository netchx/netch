using System.Collections.Generic;
using System.Linq;

namespace Netch.Models.GitHubRelease
{
    public static class VersionUtil
    {
        public static Release GetLatestRelease(IEnumerable<Release> releases, bool isPreRelease)
        {
            if (!isPreRelease)
                releases = releases.Where(release => !release.prerelease);

            releases = releases.Where(release => IsVersionString(release.tag_name));
            var ordered = releases.OrderByDescending(release => release.tag_name, new VersionComparer());
            return ordered.ElementAt(0);
        }

        private static bool IsVersionString(string str)
        {
            return SuffixVersion.TryParse(str, out _);
        }

        /// <returns> =0:versions are equal</returns>
        /// <returns> &gt;0:version1 is greater</returns>
        /// <returns> &lt;0:version2 is greater</returns>
        public static int CompareVersion(string v1, string v2)
        {
            var version1 = SuffixVersion.Parse(v1);
            var version2 = SuffixVersion.Parse(v2);
            return version1.CompareTo(version2);
        }
    }
}