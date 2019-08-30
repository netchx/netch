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
        ///     服务器
        /// </summary>
        public static readonly string SERVER_DAT = $"{DATA_DIR}\\server.dat";

        /// <summary>
        ///     订阅链接
        /// </summary>
        public static readonly string LINK_DAT = $"{DATA_DIR}\\link.dat";

        /// <summary>
        ///     设置
        /// </summary>
        public static readonly string SETTINGS_DAT = $"{DATA_DIR}\\settings.dat";

        public static readonly string TUNTAP_INI = $"{DATA_DIR}\\tuntap.ini";

        public static readonly string BYPASS_DAT = $"{DATA_DIR}\\bypass.dat";

        /// <summary>
        ///     加载配置
        /// </summary>
        public static void Load()
        {
            if (Directory.Exists(DATA_DIR))
            {
                if (File.Exists(TUNTAP_INI))
                {
                    var parser = new IniParser.FileIniDataParser();
                    var data = parser.ReadFile(TUNTAP_INI);

                    if (IPAddress.TryParse(data["Generic"]["Address"], out var address))
                    {
                        Global.TUNTAP.Address = address;
                    }

                    if (IPAddress.TryParse(data["Generic"]["Netmask"], out var netmask))
                    {
                        Global.TUNTAP.Netmask = netmask;
                    }

                    if (IPAddress.TryParse(data["Generic"]["Gateway"], out var gateway))
                    {
                        Global.TUNTAP.Gateway = gateway;
                    }

                    var dns = new List<IPAddress>();
                    foreach (var ip in data["Generic"]["DNS"].Split(','))
                    {
                        if (IPAddress.TryParse(ip, out var value))
                        {
                            dns.Add(value);
                        }
                    }

                    if (Boolean.TryParse(data["Generic"]["UseCustomDNS"], out var useCustomDNS))
                    {
                        Global.TUNTAP.UseCustomDNS = useCustomDNS;
                    }

                    if (dns.Count > 0)
                    {
                        Global.TUNTAP.DNS = dns;
                    }
                }

                if (File.Exists(BYPASS_DAT))
                {
                    Global.BypassIPs = Newtonsoft.Json.JsonConvert.DeserializeObject<List<String>>(File.ReadAllText(BYPASS_DAT));
                }

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
            if (!File.Exists(TUNTAP_INI))
            {
                File.WriteAllBytes(TUNTAP_INI, Properties.Resources.defaultTUNTAP);
            }

            var parser = new IniParser.FileIniDataParser();
            var data = parser.ReadFile(TUNTAP_INI);

            data["Generic"]["Address"] = Global.TUNTAP.Address.ToString();
            data["Generic"]["Netmask"] = Global.TUNTAP.Netmask.ToString();
            data["Generic"]["Gateway"] = Global.TUNTAP.Gateway.ToString();

            var dns = "";
            foreach (var ip in Global.TUNTAP.DNS)
            {
                dns += ip.ToString();
                dns += ',';
            }
            dns = dns.Trim();
            data["Generic"]["DNS"] = dns.Substring(0, dns.Length - 1);

            data["Generic"]["UseCustomDNS"] = Global.TUNTAP.UseCustomDNS.ToString();

            parser.WriteFile(TUNTAP_INI, data);

            File.WriteAllText(BYPASS_DAT, Newtonsoft.Json.JsonConvert.SerializeObject(Global.BypassIPs, Newtonsoft.Json.Formatting.Indented));
            File.WriteAllText(SERVER_DAT, Newtonsoft.Json.JsonConvert.SerializeObject(Global.Server, Newtonsoft.Json.Formatting.Indented));
            File.WriteAllText(LINK_DAT, Newtonsoft.Json.JsonConvert.SerializeObject(Global.SubscribeLink, Newtonsoft.Json.Formatting.Indented));
            File.WriteAllText(SETTINGS_DAT, Newtonsoft.Json.JsonConvert.SerializeObject(Global.Settings, Newtonsoft.Json.Formatting.Indented));
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
