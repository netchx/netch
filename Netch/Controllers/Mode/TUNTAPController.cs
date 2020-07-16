using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Netch.Forms;
using Netch.Models;
using Netch.Properties;
using Netch.Utils;

namespace Netch.Controllers
{
    public class TUNTAPController : ModeController
    {
        // ByPassLan IP
        private readonly List<string> _bypassLanIPs = new List<string> {"10.0.0.0/8", "172.16.0.0/16", "192.168.0.0/16"};

        private Mode _savedMode = new Mode();
        private Server _savedServer = new Server();

        /// <summary>
        ///     服务器 IP 地址
        /// </summary>
        private IPAddress[] _serverAddresses = new IPAddress[0];

        /// <summary>
        ///     本地 DNS 服务控制器
        /// </summary>
        public DNSController pDNSController = new DNSController();

        public TUNTAPController()
        {
            MainFile = "tun2socks";
            InitCheck();
        }

        /// <summary>
        ///     配置 TUNTAP 适配器
        /// </summary>
        public bool Configure()
        {
            // 查询服务器 IP 地址
            var destination = Dns.GetHostAddressesAsync(_savedServer.Hostname);
            if (destination.Wait(1000))
            {
                if (destination.Result.Length == 0) return false;

                _serverAddresses = destination.Result;
            }

            // 搜索出口
            return SearchOutbounds();
        }

        /// <summary>
        ///     设置绕行规则
        /// </summary>
        public bool SetupBypass()
        {
            Global.MainForm.StatusText(i18N.Translate("SetupBypass"));
            Logging.Info("设置绕行规则 → 设置让服务器 IP 走直连");
            // 让服务器 IP 走直连
            foreach (var address in _serverAddresses)
                if (!IPAddress.IsLoopback(address))
                    NativeMethods.CreateRoute(address.ToString(), 32, Global.Adapter.Gateway.ToString(), Global.Adapter.Index);

            // 处理模式的绕过中国
            if (_savedMode.BypassChina)
            {
                Logging.Info("设置绕行规则 → 处理模式的绕过中国");
                using (var sr = new StringReader(Encoding.UTF8.GetString(Resources.CNIP)))
                {
                    string text;

                    while ((text = sr.ReadLine()) != null)
                    {
                        var info = text.Split('/');

                        NativeMethods.CreateRoute(info[0], int.Parse(info[1]), Global.Adapter.Gateway.ToString(), Global.Adapter.Index);
                    }
                }
            }

            Logging.Info("设置绕行规则 → 处理全局绕过 IP");
            // 处理全局绕过 IP
            foreach (var ip in Global.Settings.BypassIPs)
            {
                var info = ip.Split('/');
                var address = IPAddress.Parse(info[0]);

                if (!IPAddress.IsLoopback(address)) NativeMethods.CreateRoute(address.ToString(), int.Parse(info[1]), Global.Adapter.Gateway.ToString(), Global.Adapter.Index);
            }

            Logging.Info("设置绕行规则 → 处理绕过局域网 IP");
            // 处理绕过局域网 IP
            foreach (var ip in _bypassLanIPs)
            {
                var info = ip.Split('/');
                var address = IPAddress.Parse(info[0]);

                if (!IPAddress.IsLoopback(address)) NativeMethods.CreateRoute(address.ToString(), int.Parse(info[1]), Global.Adapter.Gateway.ToString(), Global.Adapter.Index);
            }

            if (_savedMode.Type == 2) // 处理仅规则内走直连
            {
                Logging.Info("设置绕行规则 → 处理仅规则内走直连");
                // 将 TUN/TAP 网卡权重放到最高
                var instance = new Process
                {
                    StartInfo =
                    {
                        FileName = "netsh",
                        Arguments = string.Format("interface ip set interface {0} metric=0", Global.TUNTAP.Index),
                        WindowStyle = ProcessWindowStyle.Hidden,
                        UseShellExecute = true,
                        CreateNoWindow = true
                    }
                };
                instance.Start();

                Logging.Info("设置绕行规则 → 创建默认路由");
                // 创建默认路由
                if (!NativeMethods.CreateRoute("0.0.0.0", 0, Global.Settings.TUNTAP.Gateway, Global.TUNTAP.Index, 10))
                {
                    State = State.Stopped;

                    foreach (var address in _serverAddresses) NativeMethods.DeleteRoute(address.ToString(), 32, Global.Adapter.Gateway.ToString(), Global.Adapter.Index);

                    return false;
                }

                Logging.Info("设置绕行规则 → 创建规则路由");
                // 创建规则路由
                foreach (var ip in _savedMode.Rule)
                {
                    var info = ip.Split('/');

                    if (info.Length == 2)
                        if (int.TryParse(info[1], out var prefix))
                            NativeMethods.CreateRoute(info[0], prefix, Global.Adapter.Gateway.ToString(), Global.Adapter.Index);
                }
            }
            else if (_savedMode.Type == 1) // 处理仅规则内走代理
            {
                Logging.Info("设置绕行规则->处理仅规则内走代理");
                foreach (var ip in _savedMode.Rule)
                {
                    var info = ip.Split('/');

                    if (info.Length == 2)
                        if (int.TryParse(info[1], out var prefix))
                            NativeMethods.CreateRoute(info[0], prefix, Global.Settings.TUNTAP.Gateway, Global.TUNTAP.Index);
                }

                //处理 NAT 类型检测，由于协议的原因，无法仅通过域名确定需要代理的 IP，自己记录解析了返回的 IP，仅支持默认检测服务器
                if (Global.Settings.STUN_Server == "stun.stunprotocol.org")
                    try
                    {
                        var nttAddress = Dns.GetHostAddresses(Global.Settings.STUN_Server)[0];
                        if (int.TryParse("32", out var prefix)) NativeMethods.CreateRoute(nttAddress.ToString(), prefix, Global.Settings.TUNTAP.Gateway, Global.TUNTAP.Index);

                        var nttrAddress = Dns.GetHostAddresses("stunresponse.coldthunder11.com")[0];
                        if (int.TryParse("32", out var prefixr)) NativeMethods.CreateRoute(nttrAddress.ToString(), prefixr, Global.Settings.TUNTAP.Gateway, Global.TUNTAP.Index);
                    }
                    catch
                    {
                        Logging.Info("NAT 类型测试域名解析失败，将不会被添加到代理列表");
                    }

                //处理DNS代理
                if (Global.Settings.TUNTAP.ProxyDNS)
                {
                    Logging.Info("设置绕行规则 → 处理自定义 DNS 代理");
                    if (Global.Settings.TUNTAP.UseCustomDNS)
                    {
                        var dns = "";
                        foreach (var value in Global.Settings.TUNTAP.DNS)
                        {
                            dns += value;
                            dns += ',';
                        }

                        dns = dns.Trim();
                        dns = dns.Substring(0, dns.Length - 1);
                        if (int.TryParse("32", out var prefix)) NativeMethods.CreateRoute(dns, prefix, Global.Settings.TUNTAP.Gateway, Global.TUNTAP.Index);
                    }
                    else
                    {
                        if (int.TryParse("32", out var prefix))
                        {
                            NativeMethods.CreateRoute("1.1.1.1", prefix, Global.Settings.TUNTAP.Gateway, Global.TUNTAP.Index);
                            NativeMethods.CreateRoute("8.8.8.8", prefix, Global.Settings.TUNTAP.Gateway, Global.TUNTAP.Index);
                            NativeMethods.CreateRoute("9.9.9.9", prefix, Global.Settings.TUNTAP.Gateway, Global.TUNTAP.Index);
                            NativeMethods.CreateRoute("185.222.222.222", prefix, Global.Settings.TUNTAP.Gateway, Global.TUNTAP.Index);
                        }
                    }
                }
            }

            return true;
        }


        /// <summary>
        ///     清除绕行规则
        /// </summary>
        public bool ClearBypass()
        {
            if (_savedMode.Type == 2)
            {
                NativeMethods.DeleteRoute("0.0.0.0", 0, Global.Settings.TUNTAP.Gateway, Global.TUNTAP.Index, 10);

                foreach (var ip in _savedMode.Rule)
                {
                    var info = ip.Split('/');

                    if (info.Length == 2)
                        if (int.TryParse(info[1], out var prefix))
                            NativeMethods.DeleteRoute(info[0], prefix, Global.Adapter.Gateway.ToString(), Global.Adapter.Index);
                }
            }
            else if (_savedMode.Type == 1)
            {
                foreach (var ip in _savedMode.Rule)
                {
                    var info = ip.Split('/');

                    if (info.Length == 2)
                        if (int.TryParse(info[1], out var prefix))
                            NativeMethods.DeleteRoute(info[0], prefix, Global.Settings.TUNTAP.Gateway, Global.TUNTAP.Index);
                }

                if (Global.Settings.STUN_Server == "stun.stunprotocol.org")
                    try
                    {
                        var nttAddress = Dns.GetHostAddresses(Global.Settings.STUN_Server)[0];
                        if (int.TryParse("32", out var prefix)) NativeMethods.DeleteRoute(nttAddress.ToString(), prefix, Global.Settings.TUNTAP.Gateway, Global.TUNTAP.Index);

                        var nttrAddress = Dns.GetHostAddresses("stunresponse.coldthunder11.com")[0];
                        if (int.TryParse("32", out var prefixr)) NativeMethods.DeleteRoute(nttrAddress.ToString(), prefixr, Global.Settings.TUNTAP.Gateway, Global.TUNTAP.Index);
                    }
                    catch
                    {
                    }

                if (Global.Settings.TUNTAP.ProxyDNS)
                {
                    if (Global.Settings.TUNTAP.UseCustomDNS)
                    {
                        var dns = "";
                        foreach (var value in Global.Settings.TUNTAP.DNS)
                        {
                            dns += value;
                            dns += ',';
                        }

                        dns = dns.Trim();
                        dns = dns.Substring(0, dns.Length - 1);
                        if (int.TryParse("32", out var prefix)) NativeMethods.DeleteRoute(dns, prefix, Global.Settings.TUNTAP.Gateway, Global.TUNTAP.Index);
                    }
                    else
                    {
                        if (int.TryParse("32", out var prefix)) NativeMethods.DeleteRoute("1.1.1.1", prefix, Global.Settings.TUNTAP.Gateway, Global.TUNTAP.Index);
                    }
                }
            }

            foreach (var ip in Global.Settings.BypassIPs)
            {
                var info = ip.Split('/');
                var address = IPAddress.Parse(info[0]);

                if (!IPAddress.IsLoopback(address)) NativeMethods.DeleteRoute(address.ToString(), int.Parse(info[1]), Global.Adapter.Gateway.ToString(), Global.Adapter.Index);
            }

            foreach (var ip in _bypassLanIPs)
            {
                var info = ip.Split('/');
                var address = IPAddress.Parse(info[0]);

                if (!IPAddress.IsLoopback(address)) NativeMethods.DeleteRoute(address.ToString(), int.Parse(info[1]), Global.Adapter.Gateway.ToString(), Global.Adapter.Index);
            }

            if (_savedMode.BypassChina)
                using (var sr = new StringReader(Encoding.UTF8.GetString(Resources.CNIP)))
                {
                    string text;

                    while ((text = sr.ReadLine()) != null)
                    {
                        var info = text.Split('/');

                        NativeMethods.DeleteRoute(info[0], int.Parse(info[1]), Global.Adapter.Gateway.ToString(), Global.Adapter.Index);
                    }
                }

            foreach (var address in _serverAddresses)
                if (!IPAddress.IsLoopback(address))
                    NativeMethods.DeleteRoute(address.ToString(), 32, Global.Adapter.Gateway.ToString(), Global.Adapter.Index);

            return true;
        }

        public override bool Start(Server server, Mode mode)
        {
            if (!Ready) return false;

            Global.MainForm.StatusText(i18N.Translate("Starting Tap"));

            _savedMode = mode;
            _savedServer = server;

            if (!Configure()) return false;

            Logging.Info("设置绕行规则");
            SetupBypass();
            Logging.Info("设置绕行规则完毕");

            Instance = GetProcess("bin\\tun2socks.exe");

            var adapterName = TUNTAP.GetName(Global.TUNTAP.ComponentID);
            Logging.Info($"tun2sock使用适配器：{adapterName}");

            string dns;
            //V2ray使用Unbound本地DNS会导致查询异常缓慢故此V2ray不启动unbound而是使用自定义DNS
            //if (Global.Settings.TUNTAP.UseCustomDNS || server.Type.Equals("VMess"))
            if (Global.Settings.TUNTAP.UseCustomDNS)
            {
                dns = "";
                foreach (var value in Global.Settings.TUNTAP.DNS)
                {
                    dns += value;
                    dns += ',';
                }

                dns = dns.Trim();
                dns = dns.Substring(0, dns.Length - 1);
            }
            else
            {
                pDNSController.Start();
                dns = "127.0.0.1";
            }

            if (Global.Settings.TUNTAP.UseFakeDNS) dns += " -fakeDns";

            if (server.Type == "Socks5")
                Instance.StartInfo.Arguments = $"-proxyServer {server.Hostname}:{server.Port} -tunAddr {Global.Settings.TUNTAP.Address} -tunMask {Global.Settings.TUNTAP.Netmask} -tunGw {Global.Settings.TUNTAP.Gateway} -tunDns {dns} -tunName \"{adapterName}\"";
            else
                Instance.StartInfo.Arguments = $"-proxyServer 127.0.0.1:{Global.Settings.Socks5LocalPort} -tunAddr {Global.Settings.TUNTAP.Address} -tunMask {Global.Settings.TUNTAP.Netmask} -tunGw {Global.Settings.TUNTAP.Gateway} -tunDns {dns} -tunName \"{adapterName}\"";

            Instance.ErrorDataReceived += OnOutputDataReceived;
            Instance.OutputDataReceived += OnOutputDataReceived;

            State = State.Starting;
            Instance.Start();
            Instance.BeginErrorReadLine();
            Instance.BeginOutputReadLine();
            Instance.PriorityClass = ProcessPriorityClass.RealTime;

            for (var i = 0; i < 1000; i++)
            {
                Thread.Sleep(10);

                if (State == State.Started) return true;

                if (State == State.Stopped)
                {
                    Stop();
                    return false;
                }
            }

            return false;
        }

        /// <summary>
        ///     TUN/TAP停止
        /// </summary>
        public override void Stop()
        {
            StopInstance();
            ClearBypass();
            pDNSController.Stop();
        }

        private void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!Write(e.Data)) return;
            if (State == State.Starting)
            {
                if (e.Data.Contains("Running"))
                    State = State.Started;
                else if (e.Data.Contains("failed") || e.Data.Contains("invalid vconfig file")) State = State.Stopped;
            }
        }

        /// <summary>
        ///     搜索出口
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
                Logging.Error("GetBestRoute 搜索失败");
                return false;
            }

            Logging.Info($"搜索适配器index：{Global.Adapter.Index}");
            var AddressGot = false;
            foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces())
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

                            Logging.Error("出口无 IPv4 地址，当前只支持 IPv4 地址");
                            return false;
                        }

                        break;
                    }
                }
                catch (Exception)
                {
                    // ignored
                }

            if (!AddressGot)
            {
                Logging.Error("无法找到当前使用适配器");
                return false;
            }

            // 搜索 TUN/TAP 适配器的索引
            Global.TUNTAP.ComponentID = TUNTAP.GetComponentID();
            if (string.IsNullOrEmpty(Global.TUNTAP.ComponentID))
            {
                Logging.Error("未找到可用 TUN/TAP 适配器");
                if (MessageBoxX.Show(i18N.Translate("TUN/TAP driver is not detected. Is it installed now?"), confirm: true) == DialogResult.OK)
                {
                    Configuration.addtap();
                    //给点时间，不然立马安装完毕就查找适配器可能会导致找不到适配器ID
                    Thread.Sleep(1000);
                    Global.TUNTAP.ComponentID = TUNTAP.GetComponentID();
                }
                else
                {
                    return false;
                }

                //MessageBoxX.Show(i18N.Translate("Please install TAP-Windows and create an TUN/TAP adapter manually"));
                // return false;
            }

            foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces())
                if (adapter.Id == Global.TUNTAP.ComponentID)
                {
                    Global.TUNTAP.Adapter = adapter;
                    Global.TUNTAP.Index = adapter.GetIPProperties().GetIPv4Properties().Index;

                    Logging.Info($"找到适配器TUN/TAP：{adapter.Id}");

                    return true;
                }

            Logging.Error("无法找到出口");
            return false;
        }
    }
}