using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using Netch.Controllers;
using Netch.Enums;
using Netch.Interfaces;
using Netch.Models;
using Serilog;

namespace Netch.Utils
{
    public static class ModeHelper
    {
        public const string DisableModeDirectoryFileName = "disabled";

        private static FileSystemWatcher _fileSystemWatcher = null!;

        public static string ModeDirectoryFullName => Path.Combine(Global.NetchDir, "mode");

        public static bool SuspendWatcher
        {
            get => _fileSystemWatcher.EnableRaisingEvents;
            set => _fileSystemWatcher.EnableRaisingEvents = value;
        }

        public static void InitWatcher()
        {
            _fileSystemWatcher = new FileSystemWatcher(ModeDirectoryFullName)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
                IncludeSubdirectories = true,
                EnableRaisingEvents = true
            };

            var created = Observable.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(h => _fileSystemWatcher.Created += h,
                    h => _fileSystemWatcher.Created -= h)
                .Select(x => x.EventArgs);

            var changed = Observable.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(h => _fileSystemWatcher.Changed += h,
                    h => _fileSystemWatcher.Changed -= h)
                .Select(x => x.EventArgs);

            var deleted = Observable.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(h => _fileSystemWatcher.Deleted += h,
                    h => _fileSystemWatcher.Deleted -= h)
                .Select(x => x.EventArgs);

            var renamed = Observable.FromEventPattern<RenamedEventHandler, RenamedEventArgs>(h => _fileSystemWatcher.Renamed += h,
                    h => _fileSystemWatcher.Renamed -= h)
                .Select(x => x.EventArgs);

            var o = Observable.Merge(created, deleted, renamed, changed);
            o.Throttle(TimeSpan.FromSeconds(3)).Subscribe(_ => OnModeChange(), exception => Log.Error(exception, "FileSystemWatcherError"));
        }

        private static void OnModeChange()
        {
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

        public static void Load()
        {
            Global.Modes.Clear();
            LoadCore(ModeDirectoryFullName);
            Sort();
        }

        private static void LoadCore(string modeDirectory)
        {
            try
            {
                foreach (var directory in Directory.GetDirectories(modeDirectory))
                    LoadCore(directory);

                // skip Directory with a disabled file in
                if (File.Exists(Path.Combine(modeDirectory, DisableModeDirectoryFileName)))
                    return;

                foreach (var file in Directory.GetFiles(modeDirectory).Where(f => f.EndsWith(".txt")))
                    try
                    {
                        Global.Modes.Add(new Mode(file));
                    }
                    catch (Exception e)
                    {
                        Log.Warning(e, "Load mode \"{FileName}\" failed", file);
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

            Global.MainForm.ModeComboBox.Items.Remove(mode);
            Global.Modes.Remove(mode);

            if (File.Exists(mode.FullName))
                File.Delete(mode.FullName);
        }

        public static (IModeController, ModeFeature) GetModeControllerByType(ModeType type, out ushort? port, out string portName)
        {
            port = null;
            portName = string.Empty;
            switch (type)
            {
                case ModeType.Process:
                    return (new NFController(), ModeFeature.SupportIPv6 | ModeFeature.SupportSocks5Auth | ModeFeature.RequireTestNat);
                case ModeType.ProxyRuleIPs:
                    return (new TUNController(), ModeFeature.SupportSocks5Auth);
                case ModeType.BypassRuleIPs:
                    return (new TUNController(), ModeFeature.SupportSocks5Auth | ModeFeature.RequireTestNat);
                case ModeType.Pcap2Socks:
                    return (new PcapController(), 0);
                default:
                    Log.Error("未知模式类型");
                    throw new MessageException("未知模式类型");
            }
        }
    }
}