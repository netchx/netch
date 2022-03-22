using System.Diagnostics;
using System.Management;

namespace Netch.Utils;

public static class SystemInfo
{
    public static IEnumerable<string> SystemDrivers(bool allDriver)
    {
        var mc = new ManagementClass("Win32_SystemDriver");
        foreach (var obj in mc.GetInstances().Cast<ManagementObject>())
        {
            if (!(bool)obj["Started"])
                continue;

            var path = obj["PathName"].ToString();
            if (path == null)
                continue;

            var vendorExclude = new[] { "microsoft", "intel", "amd", "nvidia", "realtek" };
            var vendorName = FileVersionInfo.GetVersionInfo(path).LegalCopyright ?? string.Empty;
            if (!allDriver && vendorExclude.Any(s => vendorName.Contains(s, StringComparison.OrdinalIgnoreCase)))
                continue;

            yield return $"{obj["Caption"]} [{obj["Description"]}]({vendorName})\n\t{obj["PathName"]}";
        }
    }

    public static IEnumerable<string> Processes(bool mask)
    {
        var sortedSet = new SortedSet<string>();
        var windowsFolder = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        var windowsAppsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "WindowsApps");
        var userProfileFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        foreach (var process in Process.GetProcesses())
        {
            try
            {
                var path = process.MainModule?.FileName;
                if (path == null)
                    continue;

                if (path.StartsWith(windowsFolder, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (path.StartsWith(windowsAppsFolder, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (mask)
                    sortedSet.Add(path.Replace(userProfileFolder, "%USERPROFILE%"));
                else
                    sortedSet.Add(path);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        return sortedSet;
    }
}