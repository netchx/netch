using System.Collections.Generic;
using System.IO;

namespace Netch.Utils
{
    public static class Configuration
    {

        private static readonly string DATA_DIR = "data";
        private static readonly string SERVER_DAT = $"{DATA_DIR}\\server.dat";
        private static readonly string LINK_DAT = $"{DATA_DIR}\\link.dat";
        private static readonly string SETTINGS_DAT = $"{DATA_DIR}\\settings.dat";

        /// <summary>
        ///     加载配置
        /// </summary>
        public static void Load()
        {
            if (Directory.Exists(DATA_DIR))
            {
                if (File.Exists(SERVER_DAT))
                {
                    Global.Server = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Objects.Server>>(File.ReadAllText(SERVER_DAT));
                }

                if (File.Exists(LINK_DAT))
                {
                    Global.SubscribeLink = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Objects.SubscribeLink>>(File.ReadAllText(LINK_DAT));
                }

                if (File.Exists(SETTINGS_DAT))
                {
                    Global.Settings = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, int>>(File.ReadAllText(SETTINGS_DAT));
                }
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

            File.WriteAllText(SERVER_DAT, Newtonsoft.Json.JsonConvert.SerializeObject(Global.Server, Newtonsoft.Json.Formatting.Indented));
            File.WriteAllText(LINK_DAT, Newtonsoft.Json.JsonConvert.SerializeObject(Global.SubscribeLink, Newtonsoft.Json.Formatting.Indented));
            File.WriteAllText(SETTINGS_DAT, Newtonsoft.Json.JsonConvert.SerializeObject(Global.Settings, Newtonsoft.Json.Formatting.Indented));
        }
    }
}
