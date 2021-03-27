using System;

namespace Netch
{
    public static class Flags
    {
        public static readonly bool IsWindows10Upper = Environment.OSVersion.Version.Major >= 10;

        public static bool AlwaysShowNewVersionFound { get; set; }
    }
}