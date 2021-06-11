using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using DynamicData;
using Netch.Controllers;
using Netch.Enums;
using Netch.Interfaces;
using Netch.Models;
using Netch.Servers.Shadowsocks;
using Netch.Servers.Socks5;
using Serilog;

namespace Netch.Services
{
    public class ModeService
    {
        public const string DisableModeDirectoryFileName = "disabled";

        private readonly SourceCache<Mode, string> _modeList;
        private readonly Setting _setting;

        private FileSystemWatcher _fileSystemWatcher = null!;

        public ModeService(Setting setting, SourceCache<Mode, string> modeList)
        {
            _setting = setting;
            _modeList = modeList;

            Load();
        }

        private static string ModeDirectoryFullName => Path.Combine(Global.NetchDir, "mode");

        public bool SuspendWatcher
        {
            get => _fileSystemWatcher.EnableRaisingEvents;
            set => _fileSystemWatcher.EnableRaisingEvents = value;
        }

        public void InitWatcher()
        {
            _fileSystemWatcher = new FileSystemWatcher(ModeDirectoryFullName)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
                IncludeSubdirectories = true,
                EnableRaisingEvents = true
            };

            var ow = new ObservableFileSystemWatcher(_fileSystemWatcher);
            Observable.Merge(ow.Changed, ow.Created, ow.Deleted, ow.Renamed).Throttle(TimeSpan.FromSeconds(1)).Subscribe(x => Load());
        }

        public void AddMode(Mode mode)
        {
            _modeList.AddOrUpdate(mode);
        }

        public void UpdateMode(Mode mode)
        {
            _modeList.AddOrUpdate(mode);
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

        public void Load()
        {
            _modeList.Clear();
            LoadCore(ModeDirectoryFullName);
            Sort();
        }

        private void Sort()
        {
            // _modeList.Sort((a, b) => string.Compare(a.Remark, b.Remark, StringComparison.Ordinal));
        }

        public void Delete(Mode mode)
        {
            if (mode.FullName == null)
                throw new ArgumentException(nameof(mode.FullName));

            _modeList.Remove(mode);

            if (File.Exists(mode.FullName))
                File.Delete(mode.FullName);
        }

        private void LoadCore(string modeDirectory)
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
                        AddMode(new Mode(file));
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

        public static bool SkipServerController(Server server, Mode mode)
        {
            switch (mode.Type)
            {
                case ModeType.Process:
                    return server switch
                    {
                        Socks5 => true,
                        Shadowsocks shadowsocks when !shadowsocks.HasPlugin() && Global.Settings.Redirector.RedirectorSS => true,
                        _ => false
                    };
                case ModeType.ProxyRuleIPs:
                case ModeType.BypassRuleIPs:
                    return server is Socks5;
                default:
                    return false;
            }
        }

        public static IModeController GetModeControllerByType(ModeType type, out ushort? port, out string portName)
        {
            port = null;
            portName = string.Empty;
            switch (type)
            {
                case ModeType.Process:
                    return new NFController();
                case ModeType.ProxyRuleIPs:
                case ModeType.BypassRuleIPs:
                    return new TUNController();
                case ModeType.Pcap2Socks:
                    return new PcapController();
                default:
                    Log.Error("未知模式类型");
                    throw new MessageException("未知模式类型");
            }
        }

        private class SuspendWatcherD : IDisposable
        {
            private readonly ModeService _modeService;

            public SuspendWatcherD()
            {
                _modeService = DI.GetRequiredService<ModeService>();
                _modeService.SuspendWatcher = true;
            }

            public void Dispose()
            {
                _modeService.SuspendWatcher = false;
            }
        }
    }
}