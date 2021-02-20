using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Netch.Forms;
using Netch.Properties;

namespace Netch.Updater
{
    public static class Updater
    {
        public static void UpdateNetch(string updateFilePath)
        {
            var tempFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            var extractPath = Path.Combine(tempFolder, "extract");
            int exitCode;
            if ((exitCode = Extract(updateFilePath, extractPath, true, tempFolder)) != 0)
            {
                MessageBoxX.Show($"7za exit with code {exitCode}");
                return;
            }

            foreach (var file in Directory.GetFiles(Global.NetchDir, "*", SearchOption.AllDirectories))
            {
                if (new[] {"data", "mode\\Custom"}.ToList().Any(p => file.StartsWith(Path.Combine(Global.NetchDir, p))))
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

            MoveDirectory(Path.Combine(extractPath, "Netch"), Global.NetchDir, true);

            Global.Mutex.ReleaseMutex();
            Process.Start(Global.NetchExecutable);
            Global.MainForm.Exit(true);
        }

        private static int Extract(string archiveFileName, string destDirName, bool overwrite, string tempFolder = null)
        {
            tempFolder ??= Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            var temp7za = Path.Combine(tempFolder, "7za.exe");
            archiveFileName = Path.GetFullPath(archiveFileName);
            destDirName = Path.GetFullPath(destDirName);

            if (!Directory.Exists(tempFolder))
                Directory.CreateDirectory(tempFolder);

            if (!File.Exists(temp7za))
                File.WriteAllBytes(temp7za, Resources._7za);

            var argument = new StringBuilder();
            argument.Append($" x \"{archiveFileName}\" -o\"{destDirName}\" ");
            if (overwrite)
                argument.Append(" -y ");

            var process = Process.Start(new ProcessStartInfo
            {
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = temp7za,
                Arguments = argument.ToString()
            });

            process?.WaitForExit();
            return process?.ExitCode ?? 2;
        }

        public static FileInfo FindFile(string filename, string directory)
        {
            var DirStack = new Stack<string>();
            DirStack.Push(directory);

            while (DirStack.Count > 0)
            {
                var DirInfo = new DirectoryInfo(DirStack.Pop());
                try
                {
                    foreach (var DirChildInfo in DirInfo.GetDirectories())
                        DirStack.Push(DirChildInfo.FullName);
                }
                catch
                {
                    // ignored
                }

                try
                {
                    foreach (var FileChildInfo in DirInfo.GetFiles())
                        if (FileChildInfo.Name == filename)
                            return FileChildInfo;
                }
                catch
                {
                    // ignored
                }
            }

            return null;
        }

        private static void MoveDirectory(string sourceDirName, string destDirName, bool overwrite)
        {
            sourceDirName = Path.GetFullPath(sourceDirName);
            destDirName = Path.GetFullPath(destDirName);
            if (!overwrite)
            {
                Directory.Move(sourceDirName, destDirName);
            }
            else
            {
                foreach (var dir in Directory.GetDirectories(sourceDirName, "*", SearchOption.AllDirectories))
                    if (!Directory.Exists(dir))
                        Directory.CreateDirectory(dir);

                foreach (var f in Directory.GetFiles(sourceDirName, "*", SearchOption.AllDirectories))
                    try
                    {
                        File.Move(f, f.Replace(sourceDirName, destDirName));
                    }
                    catch (Exception)
                    {
                        throw new Exception("Updater wasn't able to move file: " + f);
                    }
            }
        }

        private static bool TestFileFree(string FileName)
        {
            try
            {
                File.Move(FileName, FileName);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void CleanOld()
        {
            foreach (var f in Directory.GetFiles(Global.NetchDir, "*.old", SearchOption.AllDirectories))
                try
                {
                    File.Delete(f);
                }
                catch (Exception)
                {
                    // ignored
                }
        }
    }
}