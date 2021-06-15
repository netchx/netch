using System.Runtime.InteropServices;

namespace Netch
{
    public static class NativeMethods
    {
        [DllImport("kernel32")]
        public static extern bool AllocConsole();

        [DllImport("kernel32")]
        public static extern bool AttachConsole(uint dwProcessId);
    }
}
