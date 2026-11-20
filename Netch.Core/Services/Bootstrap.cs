using System.Globalization;
using Microsoft.VisualStudio.Threading;
using Netch.Controllers;
using Netch.Utils;
using Serilog.Events;

namespace Netch.Services;

/// <summary>Process start-up steps shared by the front ends.</summary>
public static class Bootstrap
{
    public static string BinDirectory => Path.Combine(Global.NetchDir, "bin");

    /// <summary>False when Netch.App.exe was started without the rest of the release archive.</summary>
    public static bool BinariesPresent => Directory.Exists(BinDirectory) && Directory.EnumerateFileSystemEntries(BinDirectory).Any();

    /// <summary>Makes the install directory the working directory and puts .\bin on PATH.</summary>
    public static void SetWorkingDirectory()
    {
        Directory.SetCurrentDirectory(Global.NetchDir);
        Environment.SetEnvironmentVariable("PATH", $"{Environment.GetEnvironmentVariable("PATH")};{BinDirectory}");
    }

    /// <summary>Removes leftovers of a previous update and creates the data directories.</summary>
    public static void PrepareDataDirectories()
    {
        CleanLegacyOldFiles(Global.NetchDir);

        foreach (var item in new[] { "mode\\Custom", "data", "logging" })
            if (!Directory.Exists(item))
                Directory.CreateDirectory(item);
    }

    private static void CleanLegacyOldFiles(string targetPath)
    {
        if (!Directory.Exists(targetPath))
            return;

        var options = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
            AttributesToSkip = FileAttributes.ReparsePoint
        };
        foreach (var file in Directory.EnumerateFiles(targetPath, "*.old", options))
        {
            try
            {
                File.Delete(file);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                // ignored
            }
        }
    }

    /// <summary>Deletes the previous session's logs and starts the logger.</summary>
    public static void StartLogging()
    {
        if (Directory.Exists("logging"))
        {
            var directory = new DirectoryInfo("logging");

            foreach (var file in directory.GetFiles())
                file.Delete();

            foreach (var dir in directory.GetDirectories())
                dir.Delete(true);
        }

        Log.Logger = new LoggerConfiguration()
#if DEBUG
            .MinimumLevel.Verbose()
#else
            .MinimumLevel.Debug()
#endif
            .WriteTo.Async(c => c.File(Path.Combine(Global.NetchDir, Constants.LogFile),
                outputTemplate: Constants.OutputTemplate,
                rollOnFileSizeLimit: false))
            .WriteTo.Console(outputTemplate: Constants.OutputTemplate)
            .MinimumLevel.Override(@"Microsoft", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .CreateLogger();
    }

    public static async Task LogEnvironmentAsync()
    {
        Log.Information("Netch Version: {Version}", $"{UpdateChecker.Owner}/{UpdateChecker.Repo}@{UpdateChecker.Version}");
        Log.Information("OS: {OSVersion}", Environment.OSVersion);
        Log.Information("SHA256: {Hash}", $"{await Utils.Utils.Sha256CheckSumAsync(Global.NetchExecutable)}");
        Log.Information("System Language: {Language}", CultureInfo.CurrentCulture.Name);

#if RELEASE
        if (Log.IsEnabled(LogEventLevel.Debug))
        {
            Task.Run(() => Log.Debug("Third-party Drivers:\n{Drivers}", string.Join(Constants.EOF, SystemInfo.SystemDrivers(false)))).Forget();
            Task.Run(() => Log.Debug("Running Processes: \n{Processes}", string.Join(Constants.EOF, SystemInfo.Processes(false)))).Forget();
        }
#endif
    }
}
