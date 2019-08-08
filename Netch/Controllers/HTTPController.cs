using Microsoft.Win32;
using System;

namespace Netch.Controllers
{
    public class HTTPController
    {
        /// <summary>
        ///     实例
        /// </summary>
        public PrivoxyController pPrivoxyController = new PrivoxyController();

        /// <summary>
        ///		启动
        /// </summary>
        /// <param name="server">服务器</param>
        /// <param name="mode">模式</param>
        /// <returns>是否启动成功</returns>
        public bool Start(Objects.Server server, Objects.Mode mode)
        {
            try
            {
                if (server.Type == "Socks5")
                {
                    if (!String.IsNullOrWhiteSpace(server.Username) && !String.IsNullOrWhiteSpace(server.Password))
                    {
                        return false;
                    }
                    else
                    {
                        pPrivoxyController.Start(server, mode);
                    }
                }
                else
                {
                    pPrivoxyController.Start(server, mode);
                }

                if (mode.Type != 5)
                {
                    // HTTP 系统代理模式，启动系统代理
                    using (var registry = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", true))
                    {
                        registry.SetValue("ProxyEnable", 1);
                        registry.SetValue("ProxyServer", "127.0.0.1:2802");

                        Win32Native.InternetSetOption(IntPtr.Zero, 39, IntPtr.Zero, 0);
                        Win32Native.InternetSetOption(IntPtr.Zero, 37, IntPtr.Zero, 0);
                    }
                }
            }
            catch (Exception e)
            {
                Utils.Logging.Info(e.ToString());
                return false;
            }

            return true;
        }

        /// <summary>
        ///		停止
        /// </summary>
        public void Stop()
        {
            try
            {
                try
                {
                    pPrivoxyController.Stop();
                }
                catch (Exception e)
                {
                    Utils.Logging.Info(e.ToString());
                }

                using (var registry = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", true))
                {
                    registry.SetValue("ProxyEnable", 0);
                    registry.DeleteValue("ProxyServer", false);

                    Win32Native.InternetSetOption(IntPtr.Zero, 39, IntPtr.Zero, 0);
                    Win32Native.InternetSetOption(IntPtr.Zero, 37, IntPtr.Zero, 0);
                }
            }
            catch (Exception e)
            {
                Utils.Logging.Info(e.ToString());
            }
        }
    }
}
