using System;
using System.Runtime.InteropServices;

namespace Netch
{
    public static class Win32Native
    {
        [DllImport("User32", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("WinINet")]
        public static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer, int dwBufferLength);
    }
}
