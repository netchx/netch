using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Netch.Controllers;
using Netch.Properties;
using Netch.Utils;

namespace Netch.Updater
{
    public class Updater
    {
        private static IEnumerable<string> _keepDirectory = new List<string>(new[] {"data", "mode\\Custom"});
        private readonly string _targetPath;
        private readonly string _tempFolder;
        private readonly string _updateFilePath;

        private Updater(string updateFilePath, string targetPath)
        {
            _targetPath = targetPath;
            _tempFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(_tempFolder);

            _updateFilePath = Path.GetFullPath(updateFilePath);
            _keepDirectory = _keepDirectory.Select(s => Path.Combine(targetPath, s));
        }

        private void ApplyUpdate()
        {
            var extractPath = Path.Combine(_tempFolder, "extract");
            int exitCode;
            if ((exitCode = Extract(extractPath, true)) != 0)
                throw new Exception(i18N.Translate($"7za exit with code {exitCode}"));

            MarkFilesOld();

            MoveAllFilesOver(Path.Combine(extractPath, "Netch"), _targetPath);

            Configuration.Save();
            Global.Mutex.ReleaseMutex();
            Process.Start(Global.NetchExecutable);
            Global.MainForm.Exit(true, false);
        }

        private void MarkFilesOld()
        {
            foreach (var file in Directory.GetFiles(_targetPath, "*", SearchOption.AllDirectories))
            {
                if (_keepDirectory.Any(p => file.StartsWith(p)))
                    continue;

                try
                {
                    File.Move(file, file + ".old");
                }
                catch
                {
                    throw new Exception("Updater wasn't able to rename file: " + file);
                }
            }
        }

        private int Extract(string destDirName, bool overwrite)
        {
            var temp7za = Path.Combine(_tempFolder, "7za.exe");

            if (!File.Exists(temp7za))
                File.WriteAllBytes(temp7za, Resources._7za);

            var argument = new StringBuilder();
            argument.Append($" x \"{_updateFilePath}\" -o\"{destDirName}\"");
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

        public static void DownloadAndUpdate(string downloadPath,
            string targetPath,
            DownloadProgressChangedEventHandler onDownloadProgressChanged,
            string? keyword = null)
        {
            if (!UpdateChecker.GetFileNameAndHashFromMarkdownForm(UpdateChecker.LatestRelease.body, out var fileName, out var sha256, keyword))
                throw new MessageException(i18N.Translate("parse release note failed"));

            var fileFullPath = Path.Combine(downloadPath, fileName);
            var updater = new Updater(fileFullPath, targetPath);

            if (File.Exists(fileFullPath))
            {
                if (Utils.Utils.SHA256CheckSum(fileFullPath) == sha256)
                {
                    updater.ApplyUpdate();
                    return;
                }

                File.Delete(fileFullPath);
            }

            DownloadUpdate(onDownloadProgressChanged, fileFullPath, sha256);
            updater.ApplyUpdate();
        }

        private static void DownloadUpdate(DownloadProgressChangedEventHandler onDownloadProgressChanged, string fileFullPath, string sha256)
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
    }
}