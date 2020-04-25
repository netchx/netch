using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
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
        public static bool SearchOutbounds()
        {
            Logging.Info("正在搜索出口中");

            if (Win32Native.GetBestRoute(BitConverter.ToUInt32(IPAddress.Parse("114.114.114.114").GetAddressBytes(), 0), 0, out var pRoute) == 0)
            {
                Global.Adapter.Index = pRoute.dwForwardIfIndex;
                Global.Adapter.Gateway = new IPAddress(pRoute.dwForwardNextHop);
                Logging.Info($"当前 网关 地址：{Global.Adapter.Gateway}");
            }
            else
            {
                Logging.Info("GetBestRoute 搜索失败");
                return false;
            }

            Logging.Info($"搜索适配器index：{Global.Adapter.Index}");
            var AddressGot = false;
            foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces())
            {
                try
                {
                    var adapterProperties = adapter.GetIPProperties();
                    var p = adapterProperties.GetIPv4Properties();
                    Logging.Info($"检测适配器：{adapter.Name} {adapter.Id} {adapter.Description}, index: {p.Index}");

                    // 通过索引查找对应适配器的 IPv4 地址
                    if (p.Index == Global.Adapter.Index)
                    {
                        var AdapterIPs = "";

                        foreach (var ip in adapterProperties.UnicastAddresses)
                        {
                            if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                            {
                                AddressGot = true;
                                Global.Adapter.Address = ip.Address;
                                Logging.Info($"当前出口 IPv4 地址：{Global.Adapter.Address}");
                                break;
                            }
                            AdapterIPs = $"{ip.Address} | ";
                        }

                        if (!AddressGot)
                        {
                            if (AdapterIPs.Length > 3)
                            {
                                AdapterIPs = AdapterIPs.Substring(0, AdapterIPs.Length - 3);
                                Logging.Info($"所有出口地址：{AdapterIPs}");
                            }
                            Logging.Info("出口无 IPv4 地址，当前只支持 IPv4 地址");
                            return false;
                        }
                        break;
                    }

                }
                catch (Exception)
                { }
            }

            if (!AddressGot)
            {
                Logging.Info("无法找到当前使用适配器");
                return false;
            }

            // 搜索 TUN/TAP 适配器的索引
            Global.TUNTAP.ComponentID = TUNTAP.GetComponentID();
            if (string.IsNullOrEmpty(Global.TUNTAP.ComponentID))
            {
                Logging.Info("未找到可用 TUN/TAP 适配器");
                if (MessageBox.Show(i18N.Translate("TUN/TAP driver is not detected. Is it installed now?"), i18N.Translate("Information"), MessageBoxButtons.OKCancel, MessageBoxIcon.Information) == DialogResult.OK)
                {
                    addtap();
                    //给点时间，不然立马安装完毕就查找适配器可能会导致找不到适配器ID
                    Thread.Sleep(1000);
                    Global.TUNTAP.ComponentID = TUNTAP.GetComponentID();
                }
                else
                {
                    return false;
                }
                //MessageBox.Show(i18N.Translate("Please install TAP-Windows and create an TUN/TAP adapter manually"), i18N.Translate("Information"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                // return false;
            }

            foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (adapter.Id == Global.TUNTAP.ComponentID)
                {
                    Global.TUNTAP.Adapter = adapter;
                    Global.TUNTAP.Index = adapter.GetIPProperties().GetIPv4Properties().Index;

                    Logging.Info($"找到适配器：{adapter.Id}");

                    return true;
                }
            }

            Logging.Info("无法找到出口");
            return false;
        }
        /// <summary>
        /// 安装tap网卡
        /// </summary>
        public static void addtap()
        {
            Logging.Info("正在安装 TUN/TAP 适配器");
            //安装Tap Driver
            Process installProcess = new Process();
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
            Process installProcess = new Process();
            installProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            installProcess.StartInfo.FileName = Path.Combine("bin/tap-driver", "deltapall.bat");
            installProcess.Start();
            installProcess.WaitForExit();
            installProcess.Close();
        }
    }
}
