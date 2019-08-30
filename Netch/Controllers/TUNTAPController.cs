using System;
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
        public Objects.State State = Objects.State.Waiting;

        /// <summary>
        ///		服务器 IP 地址
        /// </summary>
        public IPAddress[] ServerAddresses = new IPAddress[0];

        /// <summary>
        ///     保存传入的规则
        /// </summary>
        public Objects.Server SavedServer = new Objects.Server();
        public Objects.Mode SavedMode = new Objects.Mode();


        /// <summary>
        ///     本地 DNS 服务控制器
        /// </summary>
        public DNSController pDNSController = new DNSController();

        /// <summary>
        ///     配置 TUNTAP 适配器
        /// </summary>
        public bool Configure()
        {
            // 查询服务器 IP 地址
            var destination = Dns.GetHostAddressesAsync(SavedServer.Address);
            if (destination.Wait(1000))
            {
                if (destination.Result.Length == 0)
                {
                    return false;
                }

                ServerAddresses = destination.Result;
            }

            // 搜索出口
            Utils.Configuration.SearchOutbounds();
            return true;
        }

        /// <summary>
        ///     设置绕行规则
        /// </summary>
        public bool SetupBypass()
        {
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

            // 处理全局绕过 IP
            foreach (var ip in Global.BypassIPs)
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
                // 创建默认路由
                if (!NativeMethods.CreateRoute("0.0.0.0", 0, Global.TUNTAP.Gateway.ToString(), Global.TUNTAP.Index, 10))
                {
                    State = Objects.State.Stopped;

                    foreach (var address in ServerAddresses)
                    {
                        NativeMethods.DeleteRoute(address.ToString(), 32, Global.Adapter.Gateway.ToString(), Global.Adapter.Index);
                    }

                    return false;
                }

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
                foreach (var ip in SavedMode.Rule)
                {
                    var info = ip.Split('/');

                    if (info.Length == 2)
                    {
                        if (int.TryParse(info[1], out var prefix))
                        {
                            NativeMethods.CreateRoute(info[0], prefix, Global.TUNTAP.Gateway.ToString(), Global.TUNTAP.Index);
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
                NativeMethods.DeleteRoute("0.0.0.0", 0, Global.TUNTAP.Gateway.ToString(), Global.TUNTAP.Index, 10);

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
                            NativeMethods.DeleteRoute(info[0], prefix, Global.TUNTAP.Gateway.ToString(), Global.TUNTAP.Index);
                        }
                    }
                }
            }

            foreach (var ip in Global.BypassIPs)
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
        public bool Start(Objects.Server server, Objects.Mode mode)
        {
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

            Configure();
            SetupBypass();

            Instance = new Process();
            Instance.StartInfo.WorkingDirectory = String.Format("{0}\\bin", Directory.GetCurrentDirectory());
            Instance.StartInfo.FileName = String.Format("{0}\\bin\\tun2socks.exe", Directory.GetCurrentDirectory());

            string dns;
            if (Global.TUNTAP.UseCustomDNS)
            {
                dns = "";
                foreach (var value in Global.TUNTAP.DNS)
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

            if (Global.TUNTAP.UseFakeDNS)
            {
                dns += " -fakeDns";
            }

            if (server.Type == "Socks5")
            {
                Instance.StartInfo.Arguments = String.Format("-proxyServer {0}:{1} -tunAddr {2} -tunMask {3} -tunGw {4} -tunDns {5}", server.Address, server.Port, Global.TUNTAP.Address, Global.TUNTAP.Netmask, Global.TUNTAP.Gateway, dns);
            }
            else
            {
                Instance.StartInfo.Arguments = String.Format("-proxyServer 127.0.0.1:2801 -tunAddr {0} -tunMask {1} -tunGw {2} -tunDns {3}", Global.TUNTAP.Address, Global.TUNTAP.Netmask, Global.TUNTAP.Gateway, dns);
            }

            Instance.StartInfo.CreateNoWindow = true;
            Instance.StartInfo.RedirectStandardError = true;
            Instance.StartInfo.RedirectStandardInput = true;
            Instance.StartInfo.RedirectStandardOutput = true;
            Instance.StartInfo.UseShellExecute = false;
            Instance.EnableRaisingEvents = true;
            Instance.ErrorDataReceived += OnOutputDataReceived;
            Instance.OutputDataReceived += OnOutputDataReceived;

            State = Objects.State.Starting;
            Instance.Start();
            Instance.BeginErrorReadLine();
            Instance.BeginOutputReadLine();
            Instance.PriorityClass = ProcessPriorityClass.RealTime;

            for (int i = 0; i < 1000; i++)
            {
                Thread.Sleep(10);

                if (State == Objects.State.Started)
                {
                    return true;
                }

                if (State == Objects.State.Stopped)
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

                pDNSController.Stop();
                ClearBypass();
            }
            catch (Exception e)
            {
                Utils.Logging.Info(e.ToString());
            }
        }

        public void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!String.IsNullOrEmpty(e.Data))
            {
                File.AppendAllText("logging\\tun2socks.log", String.Format("{0}\r\n", e.Data.Trim()));

                if (State == Objects.State.Starting)
                {
                    if (e.Data.Contains("Running"))
                    {
                        State = Objects.State.Started;
                    }
                    else if (e.Data.Contains("failed") || e.Data.Contains("invalid vconfig file"))
                    {
                        State = Objects.State.Stopped;
                    }
                }
            }
        }
    }
}
