using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;
using Netch.Models;
using Serilog;

namespace Netch.Utils
{
    public static class Configuration
    {
        /// <summary>
        ///     数据目录
        /// </summary>
        public static string DataDirectoryFullName => Path.Combine(Global.NetchDir, "data");

        public static string FileFullName => Path.Combine(DataDirectoryFullName, FileName);

        private static string BackupFileFullName => Path.Combine(DataDirectoryFullName, BackupFileName);

        private const string FileName = "settings.json";

        private const string BackupFileName = "settings.json.bak";

        private static readonly AsyncReaderWriterLock _lock = new(null);

        private static readonly JsonSerializerOptions JsonSerializerOptions = Global.NewCustomJsonSerializerOptions();

        static Configuration()
        {
            JsonSerializerOptions.Converters.Add(new ServerConverterWithTypeDiscriminator());
            JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        }

        public static async Task LoadAsync()
        {
            try
            {
                if (!File.Exists(FileFullName))
                {
                    await SaveAsync();
                    return;
                }

                await using var _ = await _lock.ReadLockAsync();

                if (await LoadCoreAsync(FileFullName))
                    return;

                Log.Information("尝试加载备份配置文件 {FileName}", BackupFileFullName);
                await LoadCoreAsync(BackupFileFullName);
            }
            catch (Exception e)
            {
                Log.Error(e, "加载配置异常");
                Environment.Exit(-1);
            }
        }

        private static async ValueTask<bool> LoadCoreAsync(string filename)
        {
            try
            {
                Setting settings;

                await using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
                {
                    settings = (await JsonSerializer.DeserializeAsync<Setting>(fs, JsonSerializerOptions).ConfigureAwait(false))!;
                }

                CheckSetting(settings);
                Global.Settings = settings;
                return true;
            }
            catch (Exception e)
            {
                Log.Error(e, @"从 {FileName} 加载配置异常", filename);
                return false;
            }
        }

        private static void CheckSetting(Setting settings)
        {
            settings.Profiles.RemoveAll(p => p.ServerRemark == string.Empty || p.ModeRemark == string.Empty);

            if (settings.Profiles.Any(p => settings.Profiles.Any(p1 => p1 != p && p1.Index == p.Index)))
                for (var i = 0; i < settings.Profiles.Count; i++)
                    settings.Profiles[i].Index = i;

            settings.AioDNS.ChinaDNS = Utils.HostAppendPort(settings.AioDNS.ChinaDNS);
            settings.AioDNS.OtherDNS = Utils.HostAppendPort(settings.AioDNS.OtherDNS);
        }

        /// <summary>
        ///     保存配置
        /// </summary>
        public static async Task SaveAsync()
        {
            if (_lock.IsWriteLockHeld)
                return;

            try
            {
                await using var _ = await _lock.WriteLockAsync();
                Log.Verbose("Save Configuration");

                if (!Directory.Exists(DataDirectoryFullName))
                    Directory.CreateDirectory(DataDirectoryFullName);

                var tempFile = Path.Combine(DataDirectoryFullName, FileFullName + ".tmp");
                await using (var fileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
                {
                    await JsonSerializer.SerializeAsync(fileStream, Global.Settings, JsonSerializerOptions);
                }

                await EnsureConfigFileExistsAsync();

                File.Replace(tempFile, FileFullName, BackupFileFullName);
            }
            catch (Exception e)
            {
                Log.Error(e, "保存配置异常");
            }
        }

        private static async ValueTask EnsureConfigFileExistsAsync()
        {
            if (!File.Exists(FileFullName))
            {
                await File.Create(FileFullName).DisposeAsync();
            }
        }
    }
}