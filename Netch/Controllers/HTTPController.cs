using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;
using WindowsProxy;
using Microsoft.Win32;
using Netch.Forms;
using Netch.Models;
using Netch.Servers.Socks5;
using Netch.Servers.Trojan;
using Netch.Utils;
using Netch.Utils.HttpProxyHandler;

namespace Netch.Controllers
{
    public class HTTPController : IModeController
    {
        public PrivoxyController pPrivoxyController = new();

        private string prevBypass, prevHTTP, prevPAC;
        private bool prevEnabled;

        public string Name { get; } = "HTTP";

        /// <summary>
        ///     启动
        /// </summary>
        /// <param name="mode">模式</param>
        /// <returns>是否启动成功</returns>
        public bool Start(in Mode mode)
        {
            RecordPrevious();

            try
            {
                if (pPrivoxyController.Start(MainController.Server, mode))
                    Global.Job.AddProcess(pPrivoxyController.Instance);

                if (mode.Type == 3)
                {
                    if (MainController.Server is Socks5 or Trojan && mode.BypassChina)
                    {
                        //启动PAC服务器
                        PACServerHandle.InitPACServer("127.0.0.1");
                    }
                    else
                    {
                        using var service = new ProxyService
                        {
                            Server = $"127.0.0.1:{Global.Settings.HTTPLocalPort}",
                            Bypass = string.Join(";", ProxyService.LanIp)
                        };
                        service.Global();
                    }
                }
            }
            catch (Exception e)
            {
                if (MessageBoxX.Show(i18N.Translate("Failed to set the system proxy, it may be caused by the lack of dependent programs. Do you want to jump to Netch's official website to download dependent programs?"), confirm: true) == DialogResult.OK) Process.Start("https://netch.org/#/?id=%e4%be%9d%e8%b5%96");

                Logging.Error("设置系统代理失败" + e);
                return false;
            }

            return true;
        }

        /// <summary>
        ///     停止
        /// </summary>
        public void Stop()
        {
            var tasks = new[]
            {
                Task.Run(pPrivoxyController.Stop),
                Task.Run(() =>
                {
                    using var service = new ProxyService();
                    try
                    {
                        PACServerHandle.Stop();
                        if (prevEnabled)
                        {
                            if (prevHTTP != "")
                            {
                                service.Server = prevHTTP;
                                service.Bypass = prevBypass;
                                service.Global();
                            }
                            if (prevPAC != "")
                            {
                                service.AutoConfigUrl = prevPAC;
                                service.Pac();
                            }
                        }
                        else
                        {
                            service.Direct();
                        }
                    }
                    catch (Exception e)
                    {
                        Logging.Error($"{Name} 控制器出错:\n" + e);
                    }
                })
            };
            Task.WaitAll(tasks);
        }

        private void RecordPrevious()
        {
            try
            {
                var registry = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings");
                if (registry == null)
                    throw new Exception();

                prevPAC = registry.GetValue("AutoConfigURL")?.ToString() ?? "";
                prevHTTP = registry.GetValue("ProxyServer")?.ToString() ?? "";
                prevBypass = registry.GetValue("ProxyOverride")?.ToString() ?? "";
                prevEnabled = registry.GetValue("ProxyEnable")?.Equals(1) ?? false; // HTTP Proxy Enabled

                if (prevHTTP == $"127.0.0.1:{Global.Settings.HTTPLocalPort}")
                {
                    prevEnabled = false;
                    prevHTTP = "";
                }

                if (prevPAC != "")
                    prevEnabled = true;
            }
            catch
            {
                prevEnabled = false;
                prevPAC = prevHTTP = prevBypass = "";
            }
        }
    }
}