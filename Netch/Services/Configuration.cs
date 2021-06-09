using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Netch.Models;
using Netch.Utils;
using Serilog;

namespace Netch.Services
{
    public static class Configuration
    {
        private const string FileName = "settings.json";

        private const string BackupFileName = "settings.json.bak";

        private static readonly JsonSerializerOptions JsonSerializerOptions = Global.NewDefaultJsonSerializerOptions;

        static Configuration()
        {
            JsonSerializerOptions.Converters.Add(new ServerConverterWithTypeDiscriminator());
            JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        }

        /// <summary>
        ///     数据目录
        /// </summary>
        public static string DataDirectoryFullName => Path.Combine(Global.NetchDir, "data");

        public static string FileFullName => Path.Combine(DataDirectoryFullName, FileName);

        private static string BackupFileFullName => Path.Combine(DataDirectoryFullName, BackupFileName);

        public static async Task LoadAsync()
        {
            try
            {
                if (!File.Exists(FileFullName))
                {
                    await SaveAsync();
                    return;
                }

                if (await LoadAsyncCore(FileFullName))
                    return;

                Log.Information("尝试加载备份配置文件 {FileName}", BackupFileFullName);
                await LoadAsyncCore(BackupFileFullName);
            }
            catch (Exception e)
            {
                Log.Error(e, "加载配置异常");
                Environment.Exit(-1);
            }
        }

        private static async ValueTask<bool> LoadAsyncCore(string filename)
        {
            try
            {
                await using var fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
                var settings = (await JsonSerializer.DeserializeAsync<Setting>(fs, JsonSerializerOptions))!;

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

            settings.AioDNS.ChinaDNS = Misc.HostAppendPort(settings.AioDNS.ChinaDNS);
            settings.AioDNS.OtherDNS = Misc.HostAppendPort(settings.AioDNS.OtherDNS);
        }

        /// <summary>
        ///     保存配置
        /// </summary>
        public static async Task SaveAsync()
        {
            try
            {
                if (!Directory.Exists(DataDirectoryFullName))
                    Directory.CreateDirectory(DataDirectoryFullName);

                var tempFile = Path.Combine(DataDirectoryFullName, FileFullName + ".tmp");

                await using (var fileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
                {
                    await JsonSerializer.SerializeAsync(fileStream, Global.Settings, JsonSerializerOptions);
                }

                File.Replace(tempFile, FileFullName, BackupFileFullName);
            }
            catch (Exception e)
            {
                Log.Error(e, "保存配置异常");
            }
        }
    }
}