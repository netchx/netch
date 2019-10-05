using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Windows.Forms;

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
            if (Directory.Exists(DATA_DIR))
            {
                if (File.Exists(SETTINGS_JSON))
                {
                    try
                    {
                        Global.Settings = Newtonsoft.Json.JsonConvert.DeserializeObject<Models.Setting>(File.ReadAllText(SETTINGS_JSON));
                        if (Global.Settings.Server != null && Global.Settings.Server.Count > 0)
                        {
                            // 如果是旧版 Server 类，使用旧版 Server 类进行读取
                            if (Global.Settings.Server[0].Hostname == null)
                            {
                                var LegacySettingConfig = Newtonsoft.Json.JsonConvert.DeserializeObject<Models.LegacySetting>(File.ReadAllText(SETTINGS_JSON));
                                for (int i = 0; i < LegacySettingConfig.Server.Count; i++)
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

                    catch (Newtonsoft.Json.JsonException)
                    {

                    }
                }
            }
            else
            {
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
            File.WriteAllText(SETTINGS_JSON, Newtonsoft.Json.JsonConvert.SerializeObject(Global.Settings, Newtonsoft.Json.Formatting.Indented));
        }

        /// <summary>
        ///		搜索出口
        /// </summary>
        public static void SearchOutbounds()
        {
            Logging.Info("正在搜索出口中");

            using (var client = new UdpClient("114.114.114.114", 53))
            {
                var address = ((IPEndPoint)client.Client.LocalEndPoint).Address;
                Global.Adapter.Address = address;

                Logging.Info($"当前 IP 地址：{Global.Adapter.Address}");

                var addressGeted = false;

                var adapters = NetworkInterface.GetAllNetworkInterfaces();
                foreach (var adapter in adapters)
                {
                    var properties = adapter.GetIPProperties();

                    foreach (var information in properties.UnicastAddresses)
                    {
                        if (information.Address.AddressFamily == AddressFamily.InterNetwork && Equals(information.Address, address))
                        {
                            addressGeted = true;
                        }
                    }

                    foreach (var information in properties.GatewayAddresses)
                    {
                        if (information.Address.AddressFamily == AddressFamily.InterNetwork && addressGeted)
                        {
                            Global.Adapter.Index = properties.GetIPv4Properties().Index;
                            Global.Adapter.Gateway = information.Address;

                            Logging.Info($"当前 网关 地址：{Global.Adapter.Gateway}");
                            break;
                        }
                    }

                    if (addressGeted)
                    {
                        break;
                    }
                }
            }

            // 搜索 TUN/TAP 适配器的索引
            Global.TUNTAP.ComponentID = TUNTAP.GetComponentID();
            if (String.IsNullOrEmpty(Global.TUNTAP.ComponentID))
            {
                MessageBox.Show(Utils.i18N.Translate("Please install TAP-Windows and create an TUN/TAP adapter manually"), Utils.i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                Environment.Exit(1);
            }

            var name = TUNTAP.GetName(Global.TUNTAP.ComponentID);
            foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (adapter.Name == name)
                {
                    Global.TUNTAP.Adapter = adapter;
                    Global.TUNTAP.Index = adapter.GetIPProperties().GetIPv4Properties().Index;

                    break;
                }
            }
        }
    }
}
