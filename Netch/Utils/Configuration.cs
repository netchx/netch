using System.Collections.Generic;
using System.IO;

namespace Netch.Utils
{
    public static class Configuration
    {
        /// <summary>
        ///     加载配置
        /// </summary>
        public static void Load()
        {
            if (Directory.Exists("data"))
            {
                if (File.Exists("data\\server.dat"))
                {
                    Global.Server = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Objects.Server>>(File.ReadAllText("data\\server.dat"));
                }

                if (File.Exists("data\\link.dat"))
                {
                    Global.SubscribeLink = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Objects.SubscribeLink>>(File.ReadAllText("data\\link.dat"));
                }
            }
        }

        /// <summary>
        ///     保存配置
        /// </summary>
        public static void Save()
        {
            if (!Directory.Exists("data"))
            {
                Directory.CreateDirectory("data");
            }

            File.WriteAllText("data\\server.dat", Newtonsoft.Json.JsonConvert.SerializeObject(Global.Server));
            File.WriteAllText("data\\link.dat", Newtonsoft.Json.JsonConvert.SerializeObject(Global.SubscribeLink));
        }
    }
}
