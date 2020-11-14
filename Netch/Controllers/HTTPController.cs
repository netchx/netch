using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using Netch.Models;
using Netch.Servers.Socks5;
using Netch.Utils;

namespace Netch.Controllers
{
    public class HTTPController : IModeController
    {
        public const string IEProxyExceptions = "localhost;127.*;10.*;172.16.*;172.17.*;172.18.*;172.19.*;172.20.*;172.21.*;172.22.*;172.23.*;172.24.*;172.25.*;172.26.*;172.27.*;172.28.*;172.29.*;172.30.*;172.31.*;192.168.*";

        public PrivoxyController pPrivoxyController = new PrivoxyController();

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
                if (pPrivoxyController.Start(MainController.ServerController.Server, mode))
                {
                    Global.Job.AddProcess(pPrivoxyController.Instance);
                }

                if (mode.Type == 3) NativeMethods.SetGlobal($"127.0.0.1:{Global.Settings.HTTPLocalPort}", IEProxyExceptions);
            }
            catch (Exception e)
            {
                if (MessageBoxX.Show(i18N.Translate("Failed to set the system proxy, it may be caused by the lack of dependent programs. Do you want to jump to Netch's official website to download dependent programs?"), confirm: true) == DialogResult.OK) Process.Start("https://netch.org/#/?id=%e4%be%9d%e8%b5%96");

                Logging.Error("设置系统代理失败" + e);
                return false;
            }

            return true;
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

        /// <summary>
        ///     停止
        /// </summary>
        public void Stop()
        {
            var tasks = new[]
            {
                Task.Factory.StartNew(pPrivoxyController.Stop),
                Task.Factory.StartNew(() =>
                {
                    if (prevEnabled)
                    {
                        if (prevHTTP != "")
                            NativeMethods.SetGlobal(prevHTTP, prevBypass);
                        if (prevPAC != "")
                            NativeMethods.SetURL(prevPAC);
                    }
                    else
                        NativeMethods.SetDIRECT();
                })
            };
            Task.WaitAll(tasks);
        }
    }
}