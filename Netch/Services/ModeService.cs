using System;
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

        private readonly SourceCache<Mode, string> _modeCache;
        private readonly Setting _setting;

        public ModeService(Setting setting, SourceCache<Mode, string> modeCache)
        {
            _setting = setting;
            _modeCache = modeCache;

            InitWatcher();
            Load();
        }

        #region FileSystemWatcher

        private FileSystemWatcher _fileSystemWatcher = null!;

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
            Observable.Merge(ow.Changed, ow.Created, ow.Deleted, ow.Renamed)
                //
                .Throttle(TimeSpan.FromSeconds(1))
                .Subscribe(x => Load());
        }

        #endregion

        private void AddMode(Mode mode)
        {
            _modeCache.AddOrUpdate(mode);
        }

        public void UpdateMode(Mode mode)
        {
            _modeCache.AddOrUpdate(mode);
            try
            {
                SuspendWatcher = true;
                mode.WriteFile();
            }
            finally
            {
                SuspendWatcher = false;
            }
        }

        public void DeleteMode(Mode mode)
        {
            if (mode.FullName == null)
                throw new ArgumentException(nameof(mode.FullName));

            _modeCache.Remove(mode);

            if (File.Exists(mode.FullName))
                File.Delete(mode.FullName);
        }

        public void Load()
        {
            _modeCache.Clear();
            LoadCore(ModeDirectoryFullName);
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

        #region Statics

        private static string ModeDirectoryFullName => Path.Combine(Global.NetchDir, "mode");

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

        #endregion

        public void CreateMode(Mode mode)
        {
            try
            {
                SuspendWatcher = true;
                AddMode(mode);
                mode.WriteFile();
            }
            finally
            {
                SuspendWatcher = false;
            }
        }
    }
}