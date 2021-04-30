using Netch.Models;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Netch.Utils
{
    public static class Configuration
    {
        /// <summary>
        ///     数据目录
        /// </summary>
        public static string DataDirectoryFullName => Path.Combine(Global.NetchDir, "data");

        public static string SettingFileFullName => $"{DataDirectoryFullName}\\settings.json";

        private static readonly JsonSerializerOptions JsonSerializerOptions = Global.NewDefaultJsonSerializerOptions;

        static Configuration()
        {
            JsonSerializerOptions.Converters.Add(new ServerConverterWithTypeDiscriminator());
            JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        }

        /// <summary>
        ///     加载配置
        /// </summary>
        public static void Load()
        {
            if (File.Exists(SettingFileFullName))
            {
                try
                {
                    using var fileStream = File.OpenRead(SettingFileFullName);
                    var settings = JsonSerializer.DeserializeAsync<Setting>(fileStream, JsonSerializerOptions).Result!;

                    CheckSetting(settings);

                    Global.Settings = settings;
                }
                catch (Exception e)
                {
                    Global.Logger.Error(e.ToString());
                    Global.Logger.ShowLog();
                    Environment.Exit(-1);
                    Global.Settings = null!;
                }
            }
            else
            {
                // 保存默认设置
                Save();
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
        public static void Save()
        {
            if (!Directory.Exists(DataDirectoryFullName))
                Directory.CreateDirectory(DataDirectoryFullName);

            using var fileStream = File.Create(SettingFileFullName);
            JsonSerializer.SerializeAsync(fileStream, Global.Settings, JsonSerializerOptions).Wait();
        }
    }
}