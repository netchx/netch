using System;
using Netch.Controllers;

namespace Netch
{
    public static class Flags
    {
        public static readonly bool IsWindows10Upper = Environment.OSVersion.Version.Major >= 10;

        private static readonly Lazy<bool> LazySupportFakeDns = new(() => new TUNTAPController().TestFakeDNS());

        public static bool SupportFakeDns => LazySupportFakeDns.Value;

        public static bool AlwaysShowNewVersionFound { get; set; }
    }
}