using System.Diagnostics;
using System.IO;

namespace Netch.Utils
{
    public static class Utils
    {
        public static bool OpenUrl(string path)
        {
            try
            {
                new Process
                {
                    StartInfo = new ProcessStartInfo(path)
                    {
                        UseShellExecute = true
                    }
                }.Start();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool OpenDir(string dir)
        {
            if (Directory.Exists(dir))
            {
                try
                {
                    return OpenUrl(dir);
                }
                catch
                {
                    // ignored
                }
            }

            return false;
        }
    }
}
