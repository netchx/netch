using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Netch.Models.GitHubRelease
{
    [Serializable]
    public struct SuffixVersion : IComparable, IComparable<SuffixVersion>
    {
        public Version Version { get; }

        public string? Suffix { get; }

        public int SuffixNum { get; }

        private SuffixVersion(Version version)
        {
            Version = version;
            Suffix = null;
            SuffixNum = 0;
        }

        private SuffixVersion(Version version, string suffix, int suffixNum)
        {
            Version = version;
            Suffix = suffix;
            SuffixNum = suffixNum;
        }

        public static SuffixVersion Parse(string? value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            var strings = value.Split('-');

            var version = Version.Parse(strings[0]);
            var suffix = strings.ElementAtOrDefault(1)?.Trim();
            switch (suffix)
            {
                case null:
                    return new SuffixVersion(version);
                case "":
                    throw new Exception("suffix WhiteSpace");
                default:
                {
                    var match = Regex.Match(suffix, @"(?<suffix>\D+)(?<num>\d+)");
                    if (!match.Success)
                        throw new Exception();

                    return new SuffixVersion(version, match.Groups["suffix"].Value, int.Parse(match.Groups["num"].Value));
                }
            }
        }

        public static bool TryParse(string? input, out SuffixVersion result)
        {
            result = default;
            try
            {
                result = Parse(input);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public int CompareTo(object? obj)
        {
            if (obj is not SuffixVersion version)
                throw new ArgumentOutOfRangeException();

            return CompareTo(version);
        }

        /// <summary>
        /// </summary>
        /// <param name="other"></param>
        /// <returns>
        ///     greater than 0 newer
        /// </returns>
        public int CompareTo(SuffixVersion other)
        {
            var versionComparison = Version.CompareTo(other.Version);
            if (versionComparison != 0)
                return versionComparison;

            var suffixExistComparison = (Suffix != null ? 1 : 0) - (other.Suffix != null ? 1 : 0);
            if (suffixExistComparison != 0)
                return suffixExistComparison;

            var suffixComparison = string.Compare(Suffix, other.Suffix, StringComparison.OrdinalIgnoreCase);
            if (suffixComparison != 0)
                return suffixComparison;

            return SuffixNum - other.SuffixNum;
        }

        public override string ToString()
        {
            var s = Version.ToString();
            if (Suffix != null)
                s += $"-{Suffix}{SuffixNum}";

            return s;
        }
    }
}