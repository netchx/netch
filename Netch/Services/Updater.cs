using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using Netch.Models;
using Netch.Properties;
using Netch.Utils;

namespace Netch.Services;

public class Updater
{
    private string UpdateFile { get; }

    private string InstallDirectory { get; }

    private readonly string _tempDirectory;
    private static readonly string[] KeepDirectories = { "data", "mode\\Custom", "logging" };
    private static readonly string[] KeepFiles = { Constants.DisableModeDirectoryFileName };

    internal Updater(string updateFile, string installDirectory)
    {
        UpdateFile = updateFile;
        InstallDirectory = installDirectory;
        _tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        Directory.CreateDirectory(_tempDirectory);
    }

    #region Apply Update

    internal void ApplyUpdate()
    {
        var extractPath = Path.Combine(_tempDirectory, "extract");

        int exitCode;
        if ((exitCode = Extract(extractPath)) != 0)
            throw new MessageException(i18N.Translate($"7za unexpectedly exited. ({exitCode})"));

        // update archive file must have a top-level directory "Netch"
        var updateDirectory = Path.Combine(extractPath, "Netch");
        if (!Directory.Exists(updateDirectory))
            throw new MessageException(i18N.Translate("Update file top-level directory not exist"));

        // {_tempDirectory}/extract/Netch/Netch.exe
        var updateMainProgramFilePath = Path.Combine(updateDirectory, "Netch.exe");
        if (!File.Exists(updateMainProgramFilePath))
            throw new MessageException(i18N.Translate($"Update file main program not exist"));

        MarkFilesOld();

        // move {tempDirectory}\extract\Netch to install folder
        MoveFilesOver(updateDirectory, InstallDirectory);
    }

    private void MarkFilesOld()
    {
        var keepDirFullPath = KeepDirectories.Select(d => Path.Combine(InstallDirectory, d)).ToImmutableList();

        foreach (var file in Directory.GetFiles(InstallDirectory, "*", SearchOption.AllDirectories))
        {
            if (keepDirFullPath.Any(p => file.StartsWith(p)))
                continue;

            if (KeepFiles.Contains(Path.GetFileName(file)))
                continue;

            try
            {
                File.Move(file, file + ".old");
            }
            catch (Exception e)
            {
                Log.Error(e, "failed to rename file \"{File}\"", file);
                throw;
            }
        }
    }

    private int Extract(string destDirName)
    {
        // release 7za.exe to {tempDirectory}\7za.exe
        var temp7za = Path.Combine(_tempDirectory, "7za.exe");

        if (!File.Exists(temp7za))
            File.WriteAllBytes(temp7za, Resources._7za);

        var argument = new StringBuilder($" x \"{UpdateFile}\" -o\"{destDirName}\" -y");
        var process = Process.Start(new ProcessStartInfo(temp7za, argument.ToString())
        {
            UseShellExecute = false
        })!;

        process.WaitForExit();
        return process.ExitCode;
    }

    private static void MoveFilesOver(string source, string target)
    {
        foreach (string directory in Directory.GetDirectories(source))
        {
            string dirName = Path.GetFileName(directory);

            if (!Directory.Exists(Path.Combine(target, dirName)))
                Directory.CreateDirectory(Path.Combine(target, dirName));

            MoveFilesOver(directory, Path.Combine(target, dirName));
        }

        foreach (string file in Directory.GetFiles(source))
        {
            var destFile = Path.Combine(target, Path.GetFileName(file));
            File.Delete(destFile);
            File.Move(file, destFile);
        }
    }

    #endregion

    #region Clean files marked as old when start

    public static void CleanOld(string targetPath)
    {
        foreach (var f in Directory.GetFiles(targetPath, "*.old", SearchOption.AllDirectories))
            try
            {
                File.Delete(f);
            }
            catch (Exception)
            {
                // ignored
            }
    }

    #endregion
}