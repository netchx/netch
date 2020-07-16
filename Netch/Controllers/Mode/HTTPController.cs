using System;
using System.Diagnostics;
using System.Windows.Forms;
using Microsoft.Win32;
using Netch.Models;
using Netch.Utils;

namespace Netch.Controllers
{
    public class HTTPController : ModeController
    {
        /// <summary>
        ///     实例
        /// </summary>
        public PrivoxyController pPrivoxyController = new PrivoxyController();

        private string prevBypass, prevHTTP, prevPAC;
        private bool prevEnabled;

        public HTTPController()
        {
            Name = "HTTP";
            Ready = true;
        }

        /// <summary>
        ///     启动
        /// </summary>
        /// <param name="server">服务器</param>
        /// <param name="mode">模式</param>
        /// <returns>是否启动成功</returns>
        public override bool Start(Server server, Mode mode)
        {
            if (!Ready) return false;

            RecordPrevious();
            try
            {
                if (server.Type == "Socks5")
                {
                    if (!string.IsNullOrWhiteSpace(server.Username) && !string.IsNullOrWhiteSpace(server.Password)) return false;

                    pPrivoxyController.Start(server, mode);
                }
                else
                {
                    pPrivoxyController.Start(server, mode);
                }

                if (mode.Type != 5) NativeMethods.SetGlobal($"127.0.0.1:{Global.Settings.HTTPLocalPort}", "<local>");
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
            var registry = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings");
            if (registry == null)
            {
                prevEnabled = false;
                prevPAC = prevHTTP = prevBypass = "";
                return;
            }

            prevPAC = registry.GetValue("AutoConfigURL")?.ToString() ?? "";
            if ((registry.GetValue("ProxyEnable")?.Equals(1) ?? false) || prevPAC != "") prevEnabled = true;

            prevHTTP = registry.GetValue("ProxyServer")?.ToString() ?? "";
            prevBypass = registry.GetValue("ProxyOverride")?.ToString() ?? "";
        }

        /// <summary>
        ///     停止
        /// </summary>
        public override void Stop()
        {
            try
            {
                pPrivoxyController.Stop();

                NativeMethods.SetGlobal(prevHTTP, prevBypass);
                if (prevPAC != "")
                    NativeMethods.SetURL(prevPAC);
                if (!prevEnabled)
                    NativeMethods.SetDIRECT();
                prevEnabled = false;
            }
            catch (Exception e)
            {
                Logging.Error("停止HTTP控制器错误：\n" + e);
            }
        }
    }
}