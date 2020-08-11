using System;
using System.Text;

namespace Netch.Models.GitHubRelease
{
    [Serializable]
    public struct SuffixVersion : ICloneable, IComparable, IComparable<SuffixVersion>, IEquatable<SuffixVersion>
    {
        public int Major { get; }
        public int Minor { get; }
        public int Patch { get; }
        public string PreRelease { get; }
        public int Build { get; }

        public SuffixVersion(int major, int minor, int patch, string preRelease, int build)
        {
            Major = major;
            Minor = minor;
            Patch = patch;
            PreRelease = preRelease;
            Build = build;
        }

        public SuffixVersion(Version version, string preRelease, int build)
        {
            Major = version.Major;
            Minor = version.Minor;
            Patch = version.Build;
            PreRelease = preRelease;
            Build = build;
        }

        public static SuffixVersion Parse(string input)
        {
            var splitStr = input.Split('-');
            var dotNetVersion = Version.Parse(splitStr[0]);
            var preRelease = new StringBuilder();
            var build = 0;

            if (splitStr.Length > 1)
                foreach (var c in splitStr[1])
                {
                    if (int.TryParse(c.ToString(), out var n))
                    {
                        build = build * 10 + n;
                    }
                    else
                    {
                        preRelease.Append(c);
                    }
                }

            return new SuffixVersion(dotNetVersion, preRelease.ToString(), build);
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


        public object Clone() => new SuffixVersion(Major, Major, Patch, PreRelease, Build);

        public int CompareTo(object obj)
        {
            if (obj is SuffixVersion version)
                return CompareTo(version);
            return -1;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        /// <returns>
        /// greater than 0 newer
        /// </returns>
        public int CompareTo(SuffixVersion other)
        {
            var majorComparison = Major.CompareTo(other.Major);
            if (majorComparison != 0)
                return majorComparison;
            var minorComparison = Minor.CompareTo(other.Minor);
            if (minorComparison != 0)
                return minorComparison;
            var patchComparison = Patch.CompareTo(other.Patch);
            if (patchComparison != 0)
                return patchComparison;
            if (PreRelease == string.Empty)
                return other.PreRelease == string.Empty ? 0 : 1;
            if (other.PreRelease == string.Empty)
                return -1;
            var suffixComparison = string.Compare(PreRelease, other.PreRelease, StringComparison.Ordinal);
            if (suffixComparison != 0)
                return suffixComparison;
            return Build.CompareTo(other.Build);
        }

        public bool Equals(SuffixVersion other)
        {
            return Major == other.Major && Minor == other.Minor && Patch == other.Patch && PreRelease == other.PreRelease && Build == other.Build;
        }

        public override bool Equals(object obj)
        {
            return obj is SuffixVersion other && Equals(other);
        }


        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Major;
                hashCode = (hashCode * 397) ^ Minor;
                hashCode = (hashCode * 397) ^ Patch;
                hashCode = (hashCode * 397) ^ (PreRelease != null ? PreRelease.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Build;
                return hashCode;
            }
        }

        public override string ToString()
        {
            return $"{Major}.{Minor}.{Patch}{(string.IsNullOrEmpty(PreRelease) ? "" : "-")}{PreRelease}{(Build == 0 ? "" : Build.ToString())}";
        }
    }
}