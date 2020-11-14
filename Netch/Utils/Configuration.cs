using System.IO;
using Netch.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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

        /// <summary>
        ///     加载配置
        /// </summary>
        public static void Load()
        {
            if (Directory.Exists(DATA_DIR) && File.Exists(SETTINGS_JSON))
            {
                try
                {
                    var settingJObject = (JObject) JsonConvert.DeserializeObject(File.ReadAllText(SETTINGS_JSON));
                    Global.Settings = settingJObject?.ToObject<Setting>() ?? new Setting();
                    Global.Settings.Server.Clear();

                    if (settingJObject?["Server"] != null)
                        foreach (JObject server in settingJObject["Server"])
                        {
                            var serverResult = ServerHelper.ParseJObject(server);
                            if (serverResult != null)
                                Global.Settings.Server.Add(serverResult);
                        }
                }
                catch (JsonException)
                {
                }
            }
            else
            {
                // 弹出提示
                i18N.Load("System");

                // 创建 data 文件夹并保存默认设置
                Save();
            }
        }

        /// <summary>
        ///     保存配置
        /// </summary>
        public static void Save()
        {
            if (!Directory.Exists(DATA_DIR))
            {
                Directory.CreateDirectory(DATA_DIR);
            }

            File.WriteAllText(SETTINGS_JSON,
                JsonConvert.SerializeObject(
                    Global.Settings,
                    Formatting.Indented,
                    new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore
                    }
                ));
        }
    }
}