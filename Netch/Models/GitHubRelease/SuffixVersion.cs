using System;
using System.Linq;

namespace Netch.Models.GitHubRelease
{
    [Serializable]
    public struct SuffixVersion : IComparable, IComparable<SuffixVersion>
    {
        public Version Version { get; }

        public string Suffix { get; }

        public SuffixVersion(Version version, string suffix)
        {
            Version = version;
            Suffix = suffix;
        }

        public static SuffixVersion Parse(string? input)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            var split = input.Split('-');
            var dotNetVersion = Version.Parse(split[0]);
            var preRelease = split.ElementAtOrDefault(1) ?? string.Empty;

            return new SuffixVersion(dotNetVersion, preRelease);
        }

        public static bool TryParse(string input, out SuffixVersion result)
        {
            try
            {
                result = Parse(input);
                return true;
            }
            catch (Exception)
            {
                result = default;
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

            if (Suffix == string.Empty)
                return other.Suffix == string.Empty ? 0 : 1;

            if (other.Suffix == string.Empty)
                return -1;

            var suffixComparison = string.Compare(Suffix, other.Suffix, StringComparison.OrdinalIgnoreCase);
            return suffixComparison;
        }

        public override string ToString()
        {
            var s = Version.ToString();
            if (Suffix != string.Empty)
                s += $"-{Suffix}";

            return s;
        }
    }
}