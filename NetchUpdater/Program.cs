using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using NetchUpdater.Properties;

namespace NetchUpdater
{
    internal class Program
    {
        private static readonly string UpdaterFullName;
        private static readonly string UpdaterDirectory;
        private static readonly string UpdaterFriendlyName;
        private static readonly Process CurrentProcess;

        static Program()
        {
            CurrentProcess = Process.GetCurrentProcess();
            UpdaterFullName = CurrentProcess.MainModule.FileName;
            UpdaterDirectory = Path.GetDirectoryName(UpdaterFullName);
            UpdaterFriendlyName = Path.GetFileName(UpdaterFullName);
        }

        public static void Main(string[] args)
        {
            var result = false;

            try
            {
                #region Check Arguments

                if (CurrentProcess.MainModule == null)
                {
                    Console.WriteLine("Current Process MainModule is null");
                    return;
                }

                if (args.Length != 3)
                {
                    Console.WriteLine("The program is not user-oriented\n此程序不是面向用户的");
                    return;
                }

                // arg0 port
                if (!int.TryParse(args[0], out var port))
                {
                    Console.WriteLine("arg0 Port Parse failed");
                    return;
                }

                var updateExtracted = true;
                // arg1 update File/Directory
                var updatePath = Path.GetFullPath(args[1]);
                if (File.Exists(updatePath))
                {
                    updateExtracted = false;
                }
                else if (!Directory.Exists(updatePath))
                {
                    Console.WriteLine("arg1 update file/directory Not found");
                    return;
                }

                // arg2 target Directory
                string targetPath;
                if (!File.Exists(Path.Combine(targetPath = Path.GetFullPath(args[2]), "Netch.exe")))
                {
                    Console.Write("arg2 Netch Directory doesn't seems right");
                    return;
                }

                #region if under target Directory,then rerun in temp directory

                if (UpdaterDirectory.StartsWith(targetPath))
                {
                    // Updater 在目标目录下
                    // 将程序复制到临时目录，传递参数
                    var tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                    var newUpdaterPath = Path.Combine(tempPath, UpdaterFriendlyName);
                    Directory.CreateDirectory(tempPath);
                    File.Copy(UpdaterFullName, newUpdaterPath);
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = newUpdaterPath,
                        Arguments = $"{port} \"{updatePath}\" \"{targetPath}\"",
                        WorkingDirectory = tempPath,
                        UseShellExecute = false
                    });
                    result = true;
                    return;
                }

                #endregion

                #endregion

                /*Console.WriteLine("Waiting Attach");
                while (!Debugger.IsAttached)
                {
                    Thread.Sleep(1000);
                }*/

                #region Send Netch Exit command

                Process[] _;
                if ((_ = Process.GetProcessesByName("Netch")).Any())
                {
                    Console.WriteLine("Found Netch process, Send exit command");
                    try
                    {
                        var udpClient = new UdpClient("127.0.0.1", port);
                        var sendBytes = Encoding.ASCII.GetBytes("Exit");
                        udpClient.Send(sendBytes, sendBytes.Length);
                    }
                    catch
                    {
                        Console.WriteLine("Send command failed");
                        return;
                    }

                    foreach (var proc in _)
                    {
                        try
                        {
                            proc.WaitForExit();
                        }
                        catch (Exception)
                        {
                            // ignored
                        }
                    }
                }

                #endregion

                var counter = 0;
                while (!TestFileFree(Path.Combine(targetPath, "Netch.exe")))
                {
                    // wait 5 sec
                    if (counter > 25)
                    {
                        Console.WriteLine("Waiting Netch exit timeout");
                        return;
                    }

                    Thread.Sleep(200);
                    counter++;
                }

                #region Update

                string extractPath = null;
                if (!updateExtracted)
                {
                    extractPath = Path.Combine(UpdaterDirectory, "extract");
                    Extract(updatePath, extractPath, true);

                    var netchExeFileInfo = FindFile("Netch.exe", extractPath);

                    if (netchExeFileInfo == null)
                    {
                        throw new Exception("Netch.exe not found in archive!");
                    }

                    updatePath = netchExeFileInfo.Directory.FullName;
                }

                MoveDirectory(updatePath, targetPath, true);

                try
                {
                    if (extractPath != null)
                        Directory.Delete(extractPath, true);
                }
                catch
                {
                    // ignored
                }

                #endregion

                #region Finished Update,Start Netch

                Console.WriteLine("Start Netch");
                Process.Start(new ProcessStartInfo
                {
                    FileName = Path.Combine(targetPath, "Netch.exe"),
                    UseShellExecute = true,
                });

                #endregion

                result = true;
            }
            catch (Exception e)
            {
                if (e is InvalidDataException)
                    Console.WriteLine("Archive file Broken");
                Console.WriteLine(e.ToString());
            }
            finally
            {
                if (!result)
                {
                    Console.WriteLine("Press any key to exit...");
                    Console.Read();
                }
            }
        }

        private static void Extract(string archiveFileName, string destDirName, bool overwrite)
        {
            archiveFileName = Path.GetFullPath(archiveFileName);
            destDirName = Path.GetFullPath(destDirName);

            if (!File.Exists("7za.exe"))
                File.WriteAllBytes("7za.exe", Resources._7za);

            var argument = new StringBuilder();
            argument.Append($" x \"{archiveFileName}\" -o\"{destDirName}\" ");
            if (overwrite)
                argument.Append(" -y ");

            Process.Start(new ProcessStartInfo
            {
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = Path.GetFullPath("7za.exe"),
                Arguments = argument.ToString()
            })?.WaitForExit();
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
                    {
                        DirStack.Push(DirChildInfo.FullName);
                    }
                }
                catch
                {
                    // ignored
                }

                try
                {
                    foreach (var FileChildInfo in DirInfo.GetFiles())
                    {
                        if (FileChildInfo.Name == filename)
                        {
                            return FileChildInfo;
                        }
                    }
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
                var dirStack = new Stack<string>();
                dirStack.Push(sourceDirName);

                while (dirStack.Count > 0)
                {
                    var dirInfo = new DirectoryInfo(dirStack.Pop());
                    try
                    {
                        foreach (var dirChildInfo in dirInfo.GetDirectories())
                        {
                            var destPath = dirChildInfo.FullName.Replace(sourceDirName, destDirName);
                            try
                            {
                                if (!Directory.Exists(destPath))
                                {
                                    Directory.CreateDirectory(destPath);
                                }
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine($"Create {destPath} Folder Error: {e.Message}");
                            }

                            dirStack.Push(dirChildInfo.FullName);
                        }

                        foreach (var fileChildInfo in dirInfo.GetFiles())
                        {
                            var destPath = fileChildInfo.FullName.Replace(sourceDirName, destDirName);
                            try
                            {
                                if (File.Exists(destPath))
                                {
                                    File.Delete(destPath);
                                }

                                fileChildInfo.MoveTo(destPath);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine($"Move {fileChildInfo.FullName} To {destPath} Error: {e.Message}");
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"ERROR:{e.Message}");
                    }
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
    }
}