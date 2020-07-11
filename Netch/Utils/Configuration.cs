using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;
using Netch.Models;
using Newtonsoft.Json;

namespace Netch.Utils
{
    public static class Configuration
    {
        /// <summary>
        ///     数据目录
        /// </summary>
        public static readonly string DATA_DIR = "data";

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
                    Global.Settings = JsonConvert.DeserializeObject<Setting>(File.ReadAllText(SETTINGS_JSON));
                    if (Global.Settings.Server != null && Global.Settings.Server.Count > 0)
                    {
                        // 如果是旧版 Server 类，使用旧版 Server 类进行读取
                        if (Global.Settings.Server[0].Hostname == null)
                        {
                            var LegacySettingConfig = JsonConvert.DeserializeObject<LegacySetting>(File.ReadAllText(SETTINGS_JSON));
                            for (var i = 0; i < LegacySettingConfig.Server.Count; i++)
                            {
                                Global.Settings.Server[i].Hostname = LegacySettingConfig.Server[i].Address;
                                if (Global.Settings.Server[i].Type == "Shadowsocks")
                                {
                                    Global.Settings.Server[i].Type = "SS";
                                    Global.Settings.Server[i].Plugin = LegacySettingConfig.Server[i].OBFS;
                                    Global.Settings.Server[i].PluginOption = LegacySettingConfig.Server[i].OBFSParam;
                                }
                                else if (Global.Settings.Server[i].Type == "ShadowsocksR")
                                {
                                    Global.Settings.Server[i].Type = "SSR";
                                }
                                else if (Global.Settings.Server[i].Type == "VMess")
                                {
                                    Global.Settings.Server[i].QUICSecure = LegacySettingConfig.Server[i].QUICSecurity;
                                }
                            }
                        }
                    }
                }

                catch (JsonException)
                {

                }
            }
            else
            {
                // 弹出提示
                MessageBoxX.Show("如果你是第一次使用本软件\n请务必前往http://netch.org 安装程序所需依赖，\n否则程序将无法正常运行！", i18N.Translate("注意！"));

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
            File.WriteAllText(SETTINGS_JSON, JsonConvert.SerializeObject(Global.Settings, Formatting.Indented));
        }

        /// <summary>
        /// 安装tap网卡
        /// </summary>
        public static void addtap()
        {
            Logging.Info("正在安装 TUN/TAP 适配器");
            //安装Tap Driver
            var installProcess = new Process();
            installProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            installProcess.StartInfo.FileName = Path.Combine("bin/tap-driver", "addtap.bat");
            installProcess.Start();
            installProcess.WaitForExit();
            installProcess.Close();
        }
        /// <summary>
        /// 卸载tap网卡
        /// </summary>
        public static void deltapall()
        {
            Logging.Info("正在卸载 TUN/TAP 适配器");
            var installProcess = new Process();
            installProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            installProcess.StartInfo.FileName = Path.Combine("bin/tap-driver", "deltapall.bat");
            installProcess.Start();
            installProcess.WaitForExit();
            installProcess.Close();
        }
    }
}
