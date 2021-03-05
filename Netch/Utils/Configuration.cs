using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Netch.Models;

namespace Netch.Utils
{
    public static class Configuration
    {
        /// <summary>
        ///     数据目录
        /// </summary>
        public const string DATA_DIR = "data";

        /// <summary>
        ///     设置
        /// </summary>
        public static readonly string SETTINGS_JSON = $"{DATA_DIR}\\settings.json";
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
            if (File.Exists(SETTINGS_JSON))
            {
                Global.Settings = ParseSetting(File.ReadAllText(SETTINGS_JSON));
            }
            else
            {
                // 弹出提示
                i18N.Load("System");

                // 创建 data 文件夹并保存默认设置
                Save();
            }
        }

        public static Setting ParseSetting(string text)
        {
            try
            {
                var settings = JsonSerializer.Deserialize<Setting>(text, JsonSerializerOptions)!;

                #region Check Profile

                settings.Profiles.RemoveAll(p => p.ServerRemark == string.Empty || p.ModeRemark == string.Empty);

                if (settings.Profiles.Any(p => settings.Profiles.Any(p1 => p1 != p && p1.Index == p.Index)))
                    for (var i = 0; i < settings.Profiles.Count; i++)
                        settings.Profiles[i].Index = i;

                #endregion

                return settings;
            }
            catch (Exception e)
            {
                Logging.Error(e.ToString());
                Utils.Open(Logging.LogFile);
                Environment.Exit(-1);
                return null!;
            }
        }

        /// <summary>
        ///     保存配置
        /// </summary>
        public static void Save()
        {
            if (!Directory.Exists(DATA_DIR))
                Directory.CreateDirectory(DATA_DIR);

            File.WriteAllBytes(SETTINGS_JSON, JsonSerializer.SerializeToUtf8Bytes(Global.Settings, JsonSerializerOptions));
        }
    }
}