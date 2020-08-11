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
            var updaterDirectory = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            string zipFile, netchDir;
            int port;

            try
            {
                if (args.Length != 3)
                {
                    Console.WriteLine("The program is not user-oriented\n此程序不是面向用户的");
                    return;
                }

                try
                {
                    var portParseResult = int.TryParse(args[0], out port);
                    if (!portParseResult)
                    {
                        Console.WriteLine("arg0 Port Parse failed");
                        return;
                    }
                }
                catch (IndexOutOfRangeException)
                {
                    Console.WriteLine("arg0 Port not specified");
                    return;
                }

                try
                {
                    var zipFileParseResult = File.Exists(zipFile = Path.GetFullPath(args[1]));
                    if (!zipFileParseResult)
                    {
                        Console.WriteLine("arg1 Zip file Not found");
                        return;
                    }
                }
                catch (IndexOutOfRangeException)
                {
                    Console.Write("arg1 Zip file not specified");
                    return;
                }

                try
                {
                    var netchDirParseResult = !File.Exists(Path.Combine(netchDir = Path.GetFullPath(args[2]), "Netch.exe"));
                    if (netchDirParseResult)
                    {
                        Console.Write("arg2 Netch Directory doesn't seems right");
                        return;
                    }
                }
                catch (IndexOutOfRangeException)
                {
                    Console.Write("arg2 Netch Directory not specified");
                    return;
                }

                if (File.Exists(Path.Combine(updaterDirectory, "Netch.exe")))
                {
                    var tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                    Directory.CreateDirectory(tempPath);
                    File.Copy(Process.GetCurrentProcess().MainModule.FileName,
                        Path.Combine(tempPath, AppDomain.CurrentDomain.FriendlyName));
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = Path.Combine(tempPath, AppDomain.CurrentDomain.FriendlyName),
                        Arguments = $"{args[0]} {args[1]} {args[2]}",
                        UseShellExecute = false
                    });

                    return;
                }

                /*if (!Debugger.IsAttached)
                {
                    Console.WriteLine("Waiting Attach");
                    Thread.Sleep(1000);
                }*/

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

                Console.WriteLine("Extract Zip");
                ExtractToDirectory(zipFile, netchDir, true);
                Console.WriteLine("Start Netch");
                Process.Start(new ProcessStartInfo
                {
                    FileName = Path.Combine(netchDir, "Netch.exe"),
                    UseShellExecute = true,
                });
            }
            catch (Exception e)
            {
                if (e is InvalidDataException)
                    Console.WriteLine("Zip file Broken");
                Console.WriteLine(e.ToString());
                Console.WriteLine("Press any key to exit...");
                Console.Read();
            }
        }

        public static void ExtractToDirectory(string archiveFileName, string destinationDirectoryName, bool overwrite)
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