using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace NetchUpdater
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var result = false;
            try
            {
                var currentProcess = Process.GetCurrentProcess();
                if (currentProcess.MainModule == null)
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

                // arg1 zipFile
                string zipFile;
                if (!File.Exists(zipFile = Path.GetFullPath(args[1])))
                {
                    Console.WriteLine("arg1 Zip file Not found");
                    return;
                }

                // arg2 target Directory
                string targetDir;
                if (!File.Exists(Path.Combine(targetDir = Path.GetFullPath(args[2]), "Netch.exe")))
                {
                    Console.Write("arg2 Netch Directory doesn't seems right");
                    return;
                }

                // check updater directory
                var updaterFullName = currentProcess.MainModule.FileName;
                var updaterDirectory = Path.GetDirectoryName(updaterFullName);
                var updaterFriendlyName = Path.GetFileName(updaterFullName);

                if (File.Exists(Path.Combine(updaterDirectory, "Netch.exe")))
                {
                    // Updater 在目标目录下
                    // 将程序复制到临时目录，传递参数
                    var tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                    var newUpdaterPath = Path.Combine(tempPath, updaterFriendlyName);
                    Directory.CreateDirectory(tempPath);
                    File.Copy(updaterFullName, newUpdaterPath);
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = newUpdaterPath,
                        Arguments = $"{args[0]} {args[1]} {args[2]}",
                        UseShellExecute = false
                    });
                    result = true;
                    return;
                }

                /*if (!Debugger.IsAttached)
                {
                    Console.WriteLine("Waiting Attach");
                    Thread.Sleep(1000);
                }*/

                // Let Netch Exit
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

                Thread.Sleep(500);

                // Extract ZIP
                Console.WriteLine("Extract Zip");
                ExtractToDirectory(zipFile, targetDir, true);

                // Start Netch
                Console.WriteLine("Start Netch");
                Process.Start(new ProcessStartInfo
                {
                    FileName = Path.Combine(targetDir, "Netch.exe"),
                    UseShellExecute = true,
                });
                result = true;
            }
            catch (Exception e)
            {
                if (e is InvalidDataException)
                    Console.WriteLine("Zip file Broken");
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

        private static void ExtractToDirectory(string archiveFileName, string destinationDirectoryName, bool overwrite)
        {
            if (!overwrite)
            {
                ZipFile.ExtractToDirectory(archiveFileName, destinationDirectoryName);
            }
            else
            {
                using (var archive = ZipFile.OpenRead(archiveFileName))
                {
                    foreach (var file in archive.Entries)
                    {
                        Console.WriteLine(file.FullName);
                        var completeFileName = Path.Combine(destinationDirectoryName, file.FullName);
                        var directory = Path.GetDirectoryName(completeFileName);

                        if (!Directory.Exists(directory) && !string.IsNullOrEmpty(directory))
                            Directory.CreateDirectory(directory);

                        if (file.Name != "")
                            file.ExtractToFile(completeFileName, true);
                    }
                }
            }
        }
    }
}