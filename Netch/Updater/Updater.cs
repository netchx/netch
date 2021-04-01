using Netch.Controllers;
using Netch.Models;
using Netch.Properties;
using Netch.Utils;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace Netch.Updater
{
    public class Updater
    {
        #region Download Update and apply update

        /// <summary>
        ///     Download Update and apply update (all arguments are FullPath)
        /// </summary>
        /// <param name="downloadDirectory"></param>
        /// <param name="installDirectory"></param>
        /// <param name="onDownloadProgressChanged"></param>
        /// <param name="keyword"></param>
        /// <exception cref="MessageException"></exception>
        public static void DownloadAndUpdate(string downloadDirectory,
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
                if (Utils.Utils.SHA256CheckSum(updateFile) == sha256)
                {
                    updater.ApplyUpdate();
                    return;
                }

                File.Delete(updateFile);
            }

            DownloadUpdateFile(onDownloadProgressChanged, updateFile, sha256);
            updater.ApplyUpdate();
        }

        /// <summary>
        ///     Download Update File
        /// </summary>
        /// <param name="onDownloadProgressChanged"></param>
        /// <param name="fileFullPath"></param>
        /// <param name="sha256"></param>
        /// <exception cref="MessageException"></exception>
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

            if (Utils.Utils.SHA256CheckSum(fileFullPath) != sha256)
                throw new MessageException(i18N.Translate("The downloaded file has the wrong hash"));
        }

        #endregion

        private readonly string _updateFile;
        private readonly string _installDirectory;
        private readonly string _tempDirectory;

        private Updater(string updateFile, string installDirectory)
        {
            _updateFile = updateFile;
            _installDirectory = installDirectory;
            _tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            Directory.CreateDirectory(_tempDirectory);
        }

        #region Apply Update

        private static readonly ImmutableArray<string> KeepDirectories = new List<string> { "data", "mode\\Custom", "logging" }.ToImmutableArray();

        private void ApplyUpdate()
        {
            var mainForm = Global.MainForm;

            #region PreUpdate

            ModeHelper.SuspendWatcher = true;
            // Stop and Save
            mainForm.Stop();
            Configuration.Save();

            // Backup Configuration file
            try
            {
                File.Copy(Configuration.SettingFileFullName, Configuration.SettingFileFullName + ".bak", true);
            }
            catch (Exception)
            {
                // ignored
            }

            #endregion

            // extract Update file to {tempDirectory}\extract
            var extractPath = Path.Combine(_tempDirectory, "extract");
            int exitCode;
            if ((exitCode = Extract(extractPath, true)) != 0)
                throw new Exception(i18N.Translate($"7za exit with code {exitCode}"));

            // rename install directory files with .old suffix unless in keep folders
            MarkFilesOld();

            // move {tempDirectory}\extract\Netch to install folder
            MoveAllFilesOver(Path.Combine(extractPath, "Netch"), _installDirectory);

            // release mutex, exit
            mainForm.Invoke(new Action(Netch.SingleInstance.Dispose));
            Process.Start(Global.NetchExecutable);
            Environment.Exit(0);
        }

        private void MarkFilesOld()
        {
            // extend keepDirectories relative path to absolute path
            var extendedKeepDirectories = KeepDirectories.Select(d => Path.Combine(_installDirectory, d)).ToImmutableArray();

            // weed out keep files
            List<string> filesToDelete = new();
            foreach (var file in Directory.GetFiles(_installDirectory, "*", SearchOption.AllDirectories))
            {
                if (extendedKeepDirectories.Any(p => file.StartsWith(p)))
                    continue;

                if (Path.GetFileName(file) is ModeHelper.DisableModeDirectoryFileName)
                    continue;

                filesToDelete.Add(file);
            }

            // rename files
            foreach (var file in filesToDelete)
                try
                {
                    File.Move(file, file + ".old");
                }
                catch
                {
                    Global.Logger.Error($"failed to rename file \"{file}\"");
                    throw;
                }
        }

        private int Extract(string destDirName, bool overwrite)
        {
            // release 7za.exe to {tempDirectory}\7za.exe
            var temp7za = Path.Combine(_tempDirectory, "7za.exe");

            if (!File.Exists(temp7za))
                File.WriteAllBytes(temp7za, Resources._7za);

            // run 7za
            var argument = new StringBuilder();
            argument.Append($" x \"{_updateFile}\" -o\"{destDirName}\"");
            if (overwrite)
                argument.Append(" -y");

            var process = Process.Start(new ProcessStartInfo
            {
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = temp7za,
                Arguments = argument.ToString()
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
}