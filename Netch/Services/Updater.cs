using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Netch.Controllers;
using Netch.Forms;
using Netch.Models;
using Netch.Properties;
using Netch.Utils;
using Serilog;

namespace Netch.Services
{
    public class Updater
    {
        private readonly string _installDirectory;
        private readonly string _tempDirectory;
        private readonly string _updateFile;

        private readonly MainForm _mainForm;
        private readonly Configuration _configuration;
        private readonly ModeService _modeService;

        private Updater(string updateFile, string installDirectory)
        {
            _updateFile = updateFile;
            _installDirectory = installDirectory;
            _tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            _configuration = DI.GetRequiredService<Configuration>();
            _mainForm = DI.GetRequiredService<MainForm>();
            _modeService = DI.GetRequiredService<ModeService>();

            Directory.CreateDirectory(_tempDirectory);
        }

        #region Download Update and apply update

        public static async Task DownloadAndUpdate(string downloadDirectory,
            string installDirectory,
            DownloadProgressChangedEventHandler onDownloadProgressChanged,
            string? keyword = null)
        {
            UpdateChecker.GetLatestUpdateFileNameAndHash(out var updateFileName, out var sha256, keyword);

            // update file Full Path
            var updateFile = Path.Combine(downloadDirectory, updateFileName);
            var updater = new Updater(updateFile, installDirectory);

            if (File.Exists(updateFile))
            {
                if (Misc.Sha256CheckSum(updateFile) == sha256)
                {
                    await updater.ApplyUpdate();
                    return;
                }

                File.Delete(updateFile);
            }

            DownloadUpdateFile(onDownloadProgressChanged, updateFile, sha256);
            await updater.ApplyUpdate();
        }

        private static void DownloadUpdateFile(DownloadProgressChangedEventHandler onDownloadProgressChanged, string fileFullPath, string sha256)
        {
            using WebClient client = new();
            try
            {
                client.DownloadProgressChanged += onDownloadProgressChanged;
                client.DownloadFile(new Uri(UpdateChecker.LatestRelease.assets[0].browser_download_url), fileFullPath);
            }
            finally
            {
                client.DownloadProgressChanged -= onDownloadProgressChanged;
            }

            if (Misc.Sha256CheckSum(fileFullPath) != sha256)
                throw new MessageException(i18N.Translate("The downloaded file has the wrong hash"));
        }

        #endregion

        #region Apply Update

        private async Task ApplyUpdate()
        {
            #region PreUpdate

            _modeService.SuspendWatcher = true;
            // Stop and Save
            await _mainForm.Stop();
            await _configuration.SaveAsync();

            #endregion

            // extract Update file to {tempDirectory}\extract
            var extractPath = Path.Combine(_tempDirectory, "extract");
            int exitCode;
            if ((exitCode = Extract(_tempDirectory, _updateFile, extractPath, true)) != 0)
                throw new MessageException(i18N.Translate($"7za unexpectedly exited. ({exitCode})"));

            var updateDirectory = Path.Combine(extractPath, "Netch");
            if (!Directory.Exists(updateDirectory))
                throw new MessageException(i18N.Translate("Update file top-level directory not exist"));

            var updateMainProgramFilePath = Path.Combine(updateDirectory, "Netch.exe");
            if (!File.Exists(updateMainProgramFilePath))
                throw new MessageException(i18N.Translate("Update file main program not exist"));


            // rename install directory files with .old suffix unless in keep folders
            MarkFilesOld(_installDirectory);

            // move {tempDirectory}\extract\Netch to install folder
            MoveAllFilesOver(updateDirectory, _installDirectory);

            // release mutex, exit
            _mainForm.Invoke(new Action(Netch.SingleInstance.Dispose));
            Process.Start(Global.NetchExecutable);
            Environment.Exit(0);
        }

        #endregion

        #region Others

        private static void MarkFilesOld(string directory)
        {
            var keepDirs = new[] { "data", "mode\\Custom", "logging" };

            var keepDirFullPath = keepDirs.Select(d => Path.Combine(directory, d)).ToImmutableList();

            foreach (var file in Directory.GetFiles(directory, "*", SearchOption.AllDirectories))
            {
                // skip keep files
                if (keepDirFullPath.Any(p => file.StartsWith(p)))
                    continue;

                // skip disable state files
                if (Path.GetFileName(file) is ModeService.DisableModeDirectoryFileName)
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

        private static int Extract(string tempDirectory, string file, string destDirName, bool overwrite)
        {
            // release 7za.exe to {tempDirectory}\7za.exe
            var temp7za = Path.Combine(tempDirectory, "7za.exe");

            if (!File.Exists(temp7za))
                File.WriteAllBytes(temp7za, Resources._7za);

            // run 7za
            var argument = new StringBuilder();
            argument.Append($" x \"{file}\" -o\"{destDirName}\"");
            if (overwrite)
                argument.Append(" -y");

            var process = Process.Start(new ProcessStartInfo(temp7za, argument.ToString())
            {
                UseShellExecute = false
            })!;

            process.WaitForExit();
            return process.ExitCode;
        }

        private static void MoveAllFilesOver(string source, string target)
        {
            foreach (string directory in Directory.GetDirectories(source))
            {
                string dirName = Path.GetFileName(directory);

                if (!Directory.Exists(Path.Combine(target, dirName)))
                    Directory.CreateDirectory(Path.Combine(target, dirName));

                MoveAllFilesOver(directory, Path.Combine(target, dirName));
            }

            foreach (string file in Directory.GetFiles(source))
            {
                var destFile = Path.Combine(target, Path.GetFileName(file));
                File.Delete(destFile);
                File.Move(file, destFile);
            }
        }

        #endregion
    }
}