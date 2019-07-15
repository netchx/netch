using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Netch.Controllers
{
    public class HTTPController
    {
        /// <summary>
        ///     实例
        /// </summary>
        public MihaZupan.HttpToSocks5Proxy Instance = null;

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
                        Instance = new MihaZupan.HttpToSocks5Proxy(server.Address, server.Port, server.Username, server.Password, 2802);
                    }
                    else
                    {
                        Instance = new MihaZupan.HttpToSocks5Proxy(server.Address, server.Port, 2802);
                    }
                }
                else
                {
                    Instance = new MihaZupan.HttpToSocks5Proxy("127.0.0.1", 2801, 2802);
                }

                using (var registry = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", true))
                {
                    registry.SetValue("ProxyEnable", 1);
                    registry.SetValue("ProxyServer", "127.0.0.1:2802");

                    Win32Native.InternetSetOption(IntPtr.Zero, 39, IntPtr.Zero, 0);
                    Win32Native.InternetSetOption(IntPtr.Zero, 37, IntPtr.Zero, 0);
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
                    if(Instance != null)
                    {
                        Instance.StopInternalServer();
                    }
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
