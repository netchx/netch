using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;

namespace Netch.Utils
{
    public static class SystemInfo
    {
        public static IEnumerable<string> SystemDrivers(bool allDriver)
        {
            var mc = new ManagementClass("Win32_SystemDriver");
            foreach (var obj in mc.GetInstances().Cast<ManagementObject>())
            {
                if (!(bool) obj["Started"])
                    continue;

                var path = obj["PathName"].ToString();
                if (path == null)
                    continue;

                var vendorExclude = new[] {"microsoft", "intel", "amd", "nvidia", "realtek"};
                var vendorName = FileVersionInfo.GetVersionInfo(path).LegalCopyright ?? string.Empty;
                if (!allDriver && vendorExclude.Any(s => vendorName.Contains(s, StringComparison.OrdinalIgnoreCase)))
                    continue;

                yield return $"{obj["Caption"]} [{obj["Description"]}]({vendorName})\n\t{obj["PathName"]}";
            }
        }

        public static IEnumerable<string> Processes(bool mask)
        {
            var hashset = new HashSet<string>();
            foreach (var process in Process.GetProcesses())
            {
                try
                {
                    if (process.Id is 0 or 4)
                        continue;

                    if (process.MainModule!.FileName!.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.Windows), StringComparison.OrdinalIgnoreCase))
                        continue;
                    var path = process.MainModule.FileName;

                    if (mask)
                        path = path.Replace(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "%USERPROFILE%");

                    hashset.Add(path);
                }
                catch (Exception)
                {
                    // ignored
                }
            }

            return hashset;
        }
    }
}