using System;
using System.IO;
using System.Linq;
using Netch.Controllers;
using Netch.Models;
using Netch.Servers.Shadowsocks;
using Netch.Servers.Socks5;

namespace Netch.Utils
{
    public static class ModeHelper
    {
        public const string DisableModeDirectoryFileName = "disabled";

        public static string ModeDirectoryFullName => Path.Combine(Global.NetchDir, "mode");

        private static readonly FileSystemWatcher FileSystemWatcher;

        public static bool SuspendWatcher { get; set; } = false;

        static ModeHelper()
        {
            FileSystemWatcher = new FileSystemWatcher(ModeDirectoryFullName)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName,
                IncludeSubdirectories = true,
                EnableRaisingEvents = true
            };

            FileSystemWatcher.Changed += OnModeChanged;
            FileSystemWatcher.Created += OnModeChanged;
            FileSystemWatcher.Deleted += OnModeChanged;
            FileSystemWatcher.Renamed += OnModeChanged;
        }

        private static void OnModeChanged(object sender, FileSystemEventArgs e)
        {
            if (SuspendWatcher)
                return;

            Load();
            Global.MainForm.LoadModes();
        }

        public static string GetRelativePath(string fullName)
        {
            var length = ModeDirectoryFullName.Length;
            if (!ModeDirectoryFullName.EndsWith("\\"))
                length++;

            return fullName.Substring(length);
        }

        public static string GetFullPath(string relativeName)
        {
            return Path.Combine(ModeDirectoryFullName, relativeName);
        }

        /// <summary>
        ///     从模式文件夹读取模式
        /// </summary>
        public static void Load()
        {
            Global.Modes.Clear();
            LoadModeDirectory(ModeDirectoryFullName);

            Sort();
        }

        private static void LoadModeDirectory(string modeDirectory)
        {
            try
            {
                foreach (var directory in Directory.GetDirectories(modeDirectory))
                    LoadModeDirectory(directory);

                // skip Directory with a disabled file in
                if (File.Exists(Path.Combine(modeDirectory, DisableModeDirectoryFileName)))
                    return;

                foreach (var file in Directory.GetFiles(modeDirectory).Where(f => f.EndsWith(".txt")))
                    try
                    {
                        Global.Modes.Add(new Mode(file));
                    }
                    catch (Exception)
                    {
                        // ignored
                    }
            }
            catch
            {
                // ignored
            }
        }

        private static void Sort()
        {
            Global.Modes.Sort((a, b) => string.Compare(a.Remark, b.Remark, StringComparison.Ordinal));
        }

        public static void Delete(Mode mode)
        {
            if (mode.FullName == null)
                throw new ArgumentException(nameof(mode.FullName));

            File.Delete(mode.FullName);
        }

        public static bool SkipServerController(Server server, Mode mode)
        {
            return mode.Type switch
                   {
                       0 => server switch
                            {
                                Socks5 => true,
                                Shadowsocks shadowsocks when !shadowsocks.HasPlugin() && Global.Settings.RedirectorSS => true,
                                _ => false
                            },
                       _ => false
                   };
        }

        public static IModeController? GetModeControllerByType(int type, out ushort? port, out string portName)
        {
            port = null;
            portName = string.Empty;
            switch (type)
            {
                case 0:
                    port = Global.Settings.RedirectorTCPPort;
                    portName = "Redirector TCP";
                    return new NFController();
                case 1:
                case 2:
                    return new TUNTAPController();
                case 3:
                case 5:
                    port = Global.Settings.HTTPLocalPort;
                    portName = "HTTP";
                    StatusPortInfoText.HttpPort = (ushort) port;
                    return new HTTPController();
                case 4:
                    return null;
                case 6:
                    return new PcapController();
                default:
                    Logging.Error("未知模式类型");
                    throw new MessageException("未知模式类型");
            }
        }
    }
}