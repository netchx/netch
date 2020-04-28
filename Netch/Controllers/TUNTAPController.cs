using Netch.Forms;
using Netch.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace Netch.Controllers
{
    public class TUNTAPController
    {
        /// <summary>
        ///		进程实例（tun2socks）
        /// </summary>
        public Process Instance;

        /// <summary>
        ///		当前状态
        /// </summary>
        public Models.State State = Models.State.Waiting;

        /// <summary>
        ///		服务器 IP 地址
        /// </summary>
        public IPAddress[] ServerAddresses = new IPAddress[0];

        /// <summary>
        ///     保存传入的规则
        /// </summary>
        public Models.Server SavedServer = new Models.Server();
        public Models.Mode SavedMode = new Models.Mode();

        /// <summary>
        ///		本地 DNS 服务控制器
        /// </summary>
        public DNSController pDNSController = new DNSController();

        // ByPassLan IP
        List<string> BypassLanIPs = new List<string>() { "10.0.0.0/8", "172.16.0.0/16", "192.168.0.0/16" };

        /// <summary>
        ///     配置 TUNTAP 适配器
        /// </summary>
        public bool Configure()
        {
            // 查询服务器 IP 地址
            var destination = Dns.GetHostAddressesAsync(SavedServer.Hostname);
            if (destination.Wait(1000))
            {
                if (destination.Result.Length == 0)
                {
                    return false;
                }

                ServerAddresses = destination.Result;
            }

            // 搜索出口
            return Utils.Configuration.SearchOutbounds();
        }

        /// <summary>
        ///     设置绕行规则
        /// </summary>
        public bool SetupBypass()
        {
            MainForm.Instance.StatusText($"{Utils.i18N.Translate("Status")}{Utils.i18N.Translate(": ")}{Utils.i18N.Translate("SetupBypass")}");
            Logging.Info("设置绕行规则->设置让服务器 IP 走直连");
            // 让服务器 IP 走直连
            foreach (var address in ServerAddresses)
            {
                if (!IPAddress.IsLoopback(address))
                {
                    NativeMethods.CreateRoute(address.ToString(), 32, Global.Adapter.Gateway.ToString(), Global.Adapter.Index);
                }
            }

            // 处理模式的绕过中国
            if (SavedMode.BypassChina)
            {
                Logging.Info("设置绕行规则->处理模式的绕过中国");
                using (var sr = new StringReader(Encoding.UTF8.GetString(Properties.Resources.CNIP)))
                {
                    string text;

                    while ((text = sr.ReadLine()) != null)
                    {
                        var info = text.Split('/');

                        NativeMethods.CreateRoute(info[0], int.Parse(info[1]), Global.Adapter.Gateway.ToString(), Global.Adapter.Index);
                    }
                }
            }

            Logging.Info("设置绕行规则->处理全局绕过 IP");
            // 处理全局绕过 IP
            foreach (var ip in Global.Settings.BypassIPs)
            {
                var info = ip.Split('/');
                var address = IPAddress.Parse(info[0]);

                if (!IPAddress.IsLoopback(address))
                {
                    NativeMethods.CreateRoute(address.ToString(), int.Parse(info[1]), Global.Adapter.Gateway.ToString(), Global.Adapter.Index);
                }
            }

            Logging.Info("设置绕行规则->处理绕过局域网 IP");
            // 处理绕过局域网 IP
            foreach (var ip in BypassLanIPs)
            {
                var info = ip.Split('/');
                var address = IPAddress.Parse(info[0]);

                if (!IPAddress.IsLoopback(address))
                {
                    NativeMethods.CreateRoute(address.ToString(), int.Parse(info[1]), Global.Adapter.Gateway.ToString(), Global.Adapter.Index);
                }
            }

            if (SavedMode.Type == 2) // 处理仅规则内走直连
            {
                Logging.Info("设置绕行规则->处理仅规则内走直连");
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

                Logging.Info("设置绕行规则->创建默认路由");
                // 创建默认路由
                if (!NativeMethods.CreateRoute("0.0.0.0", 0, Global.Settings.TUNTAP.Gateway, Global.TUNTAP.Index, 10))
                {
                    State = Models.State.Stopped;

                    foreach (var address in ServerAddresses)
                    {
                        NativeMethods.DeleteRoute(address.ToString(), 32, Global.Adapter.Gateway.ToString(), Global.Adapter.Index);
                    }

                    return false;
                }

                Logging.Info("设置绕行规则->创建规则路由");
                // 创建规则路由
                foreach (var ip in SavedMode.Rule)
                {
                    var info = ip.Split('/');

                    if (info.Length == 2)
                    {
                        if (int.TryParse(info[1], out var prefix))
                        {
                            NativeMethods.CreateRoute(info[0], prefix, Global.Adapter.Gateway.ToString(), Global.Adapter.Index);
                        }
                    }
                }
            }
            else if (SavedMode.Type == 1) // 处理仅规则内走代理
            {
                Logging.Info("设置绕行规则->处理仅规则内走代理");
                foreach (var ip in SavedMode.Rule)
                {
                    var info = ip.Split('/');

                    if (info.Length == 2)
                    {
                        if (int.TryParse(info[1], out var prefix))
                        {
                            NativeMethods.CreateRoute(info[0], prefix, Global.Settings.TUNTAP.Gateway, Global.TUNTAP.Index);
                        }
                    }
                }
                //处理NAT类型检测，由于协议的原因，无法仅通过域名确定需要代理的IP，自己记录解析了返回的IP，仅支持默认检测服务器
                if (Global.Settings.STUN_Server == "stun.stunprotocol.org")
                {
                    try
                    {
                        var nttAddress = Dns.GetHostAddresses(Global.Settings.STUN_Server)[0];
                        if (int.TryParse("32", out var prefix))
                        {
                            NativeMethods.CreateRoute(nttAddress.ToString(), prefix, Global.Settings.TUNTAP.Gateway, Global.TUNTAP.Index);
                        }
                        var nttrAddress = Dns.GetHostAddresses("stunresponse.coldthunder11.com")[0];
                        if (int.TryParse("32", out var prefixr))
                        {
                            NativeMethods.CreateRoute(nttrAddress.ToString(), prefixr, Global.Settings.TUNTAP.Gateway, Global.TUNTAP.Index);
                        }
                    }
                    catch
                    {
                        Logging.Info("NAT类型测试域名解析失败，将不会被添加到代理列表。");
                    }
                }
                //处理DNS代理
                if (Global.Settings.TUNTAP.ProxyDNS)
                {
                    Logging.Info("设置绕行规则->处理自定义DNS代理");
                    if (Global.Settings.TUNTAP.UseCustomDNS)
                    {
                        string dns = "";
                        foreach (var value in Global.Settings.TUNTAP.DNS)
                        {
                            dns += value;
                            dns += ',';
                        }

                        dns = dns.Trim();
                        dns = dns.Substring(0, dns.Length - 1);
                        if (int.TryParse("32", out var prefix))
                        {
                            NativeMethods.CreateRoute(dns, prefix, Global.Settings.TUNTAP.Gateway, Global.TUNTAP.Index);
                        }
                    }
                    else
                    {
                        if (int.TryParse("32", out var prefix))
                        {
                            NativeMethods.CreateRoute("1.1.1.1", prefix, Global.Settings.TUNTAP.Gateway, Global.TUNTAP.Index);
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
            if (SavedMode.Type == 2)
            {
                NativeMethods.DeleteRoute("0.0.0.0", 0, Global.Settings.TUNTAP.Gateway, Global.TUNTAP.Index, 10);

                foreach (var ip in SavedMode.Rule)
                {
                    var info = ip.Split('/');

                    if (info.Length == 2)
                    {
                        if (int.TryParse(info[1], out var prefix))
                        {
                            NativeMethods.DeleteRoute(info[0], prefix, Global.Adapter.Gateway.ToString(), Global.Adapter.Index);
                        }
                    }
                }
            }
            else if (SavedMode.Type == 1)
            {
                foreach (var ip in SavedMode.Rule)
                {
                    var info = ip.Split('/');

                    if (info.Length == 2)
                    {
                        if (int.TryParse(info[1], out var prefix))
                        {
                            NativeMethods.DeleteRoute(info[0], prefix, Global.Settings.TUNTAP.Gateway, Global.TUNTAP.Index);
                        }
                    }
                }
                if (Global.Settings.STUN_Server == "stun.stunprotocol.org")
                {
                    try
                    {
                        var nttAddress = Dns.GetHostAddresses(Global.Settings.STUN_Server)[0];
                        if (int.TryParse("32", out var prefix))
                        {
                            NativeMethods.DeleteRoute(nttAddress.ToString(), prefix, Global.Settings.TUNTAP.Gateway, Global.TUNTAP.Index);
                        }
                        var nttrAddress = Dns.GetHostAddresses("stunresponse.coldthunder11.com")[0];
                        if (int.TryParse("32", out var prefixr))
                        {
                            NativeMethods.DeleteRoute(nttrAddress.ToString(), prefixr, Global.Settings.TUNTAP.Gateway, Global.TUNTAP.Index);
                        }
                    }
                    catch { }
                }
                if (Global.Settings.TUNTAP.ProxyDNS)
                {
                    if (Global.Settings.TUNTAP.UseCustomDNS)
                    {
                        string dns = "";
                        foreach (var value in Global.Settings.TUNTAP.DNS)
                        {
                            dns += value;
                            dns += ',';
                        }

                        dns = dns.Trim();
                        dns = dns.Substring(0, dns.Length - 1);
                        if (int.TryParse("32", out var prefix))
                        {
                            NativeMethods.DeleteRoute(dns, prefix, Global.Settings.TUNTAP.Gateway, Global.TUNTAP.Index);
                        }
                    }
                    else
                    {
                        if (int.TryParse("32", out var prefix))
                        {
                            NativeMethods.DeleteRoute("1.1.1.1", prefix, Global.Settings.TUNTAP.Gateway, Global.TUNTAP.Index);
                        }
                    }
                }
            }

            foreach (var ip in Global.Settings.BypassIPs)
            {
                var info = ip.Split('/');
                var address = IPAddress.Parse(info[0]);

                if (!IPAddress.IsLoopback(address))
                {
                    NativeMethods.DeleteRoute(address.ToString(), int.Parse(info[1]), Global.Adapter.Gateway.ToString(), Global.Adapter.Index);
                }
            }
            foreach (var ip in BypassLanIPs)
            {
                var info = ip.Split('/');
                var address = IPAddress.Parse(info[0]);

                if (!IPAddress.IsLoopback(address))
                {
                    NativeMethods.DeleteRoute(address.ToString(), int.Parse(info[1]), Global.Adapter.Gateway.ToString(), Global.Adapter.Index);
                }
            }

            if (SavedMode.BypassChina)
            {
                using (var sr = new StringReader(Encoding.UTF8.GetString(Properties.Resources.CNIP)))
                {
                    string text;

                    while ((text = sr.ReadLine()) != null)
                    {
                        var info = text.Split('/');

                        NativeMethods.DeleteRoute(info[0], int.Parse(info[1]), Global.Adapter.Gateway.ToString(), Global.Adapter.Index);
                    }
                }
            }

            foreach (var address in ServerAddresses)
            {
                if (!IPAddress.IsLoopback(address))
                {
                    NativeMethods.DeleteRoute(address.ToString(), 32, Global.Adapter.Gateway.ToString(), Global.Adapter.Index);
                }
            }
            return true;
        }

        /// <summary>
        ///		启动
        /// </summary>
        /// <param name="server">配置</param>
        /// <returns>是否成功</returns>
        public bool Start(Models.Server server, Models.Mode mode)
        {
            MainForm.Instance.StatusText($"{Utils.i18N.Translate("Status")}{Utils.i18N.Translate(": ")}{Utils.i18N.Translate("Starting Tap")}");
            foreach (var proc in Process.GetProcessesByName("tun2socks"))
            {
                try
                {
                    proc.Kill();
                }
                catch (Exception)
                {
                    // 跳过
                }
            }

            if (!File.Exists("bin\\tun2socks.exe"))
            {
                return false;
            }

            if (File.Exists("logging\\tun2socks.log"))
            {
                File.Delete("logging\\tun2socks.log");
            }

            SavedMode = mode;
            SavedServer = server;

            if (!Configure())
            {
                return false;
            }

            Logging.Info("设置绕行规则");
            SetupBypass();
            Logging.Info("设置绕行规则完毕");

            Instance = new Process();
            Instance.StartInfo.WorkingDirectory = string.Format("{0}\\bin", Directory.GetCurrentDirectory());
            Instance.StartInfo.FileName = string.Format("{0}\\bin\\tun2socks.exe", Directory.GetCurrentDirectory());
            var adapterName = TUNTAP.GetName(Global.TUNTAP.ComponentID);
            Logging.Info($"tun2sock使用适配器：{adapterName}");

            string dns;
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
                //dns = "1.1.1.1,1.0.0.1";
            }
            if (Global.Settings.TUNTAP.UseFakeDNS)
            {
                dns += " -fakeDns";
            }

            if (server.Type == "Socks5")
            {
                Instance.StartInfo.Arguments = string.Format("-proxyServer {0}:{1} -tunAddr {2} -tunMask {3} -tunGw {4} -tunDns {5} -tunName \"{6}\"", server.Hostname, server.Port, Global.Settings.TUNTAP.Address, Global.Settings.TUNTAP.Netmask, Global.Settings.TUNTAP.Gateway, dns, adapterName);
            }
            else
            {
                Instance.StartInfo.Arguments = string.Format("-proxyServer 127.0.0.1:{0} -tunAddr {1} -tunMask {2} -tunGw {3} -tunDns {4} -tunName \"{5}\"", Global.Settings.Socks5LocalPort, Global.Settings.TUNTAP.Address, Global.Settings.TUNTAP.Netmask, Global.Settings.TUNTAP.Gateway, dns, adapterName);
            }

            Instance.StartInfo.CreateNoWindow = true;
            Instance.StartInfo.RedirectStandardError = true;
            Instance.StartInfo.RedirectStandardInput = true;
            Instance.StartInfo.RedirectStandardOutput = true;
            Instance.StartInfo.UseShellExecute = false;
            Instance.EnableRaisingEvents = true;
            Instance.ErrorDataReceived += OnOutputDataReceived;
            Instance.OutputDataReceived += OnOutputDataReceived;

            Logging.Info(Instance.StartInfo.Arguments);

            State = Models.State.Starting;
            Instance.Start();
            Instance.BeginErrorReadLine();
            Instance.BeginOutputReadLine();
            Instance.PriorityClass = ProcessPriorityClass.RealTime;

            for (var i = 0; i < 1000; i++)
            {
                Thread.Sleep(10);

                if (State == Models.State.Started)
                {
                    return true;
                }

                if (State == Models.State.Stopped)
                {
                    Stop();
                    return false;
                }
            }

            return false;
        }

        /// <summary>
        ///		停止
        /// </summary>
        public void Stop()
        {
            try
            {
                if (Instance != null && !Instance.HasExited)
                {
                    Instance.Kill();
                }

                //pDNSController.Stop();
                //修复点击停止按钮后再启动，DNS服务没监听的BUG
                ClearBypass();
                pDNSController.Stop();
            }
            catch (Exception e)
            {
                Utils.Logging.Info(e.ToString());
            }
        }

        public void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                File.AppendAllText("logging\\tun2socks.log", string.Format("{0}\r\n", e.Data.Trim()));

                if (State == Models.State.Starting)
                {
                    if (e.Data.Contains("Running"))
                    {
                        State = Models.State.Started;
                    }
                    else if (e.Data.Contains("failed") || e.Data.Contains("invalid vconfig file"))
                    {
                        State = Models.State.Stopped;
                    }
                }
            }
        }
    }
}
