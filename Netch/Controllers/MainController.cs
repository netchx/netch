using System;
using System.Diagnostics;
using System.IO;

namespace Netch.Controllers
{
    public class MainController
    {
        public static Process GetProcess()
        {
            var process = new Process();
            process.StartInfo.WorkingDirectory = String.Format("{0}\\bin", Directory.GetCurrentDirectory());
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.EnableRaisingEvents = true;

            return process;
        }

        /// <summary>
        ///		SS 控制器
        /// </summary>
        public SSController pSSController = new SSController();

        /// <summary>
        ///     SSR 控制器
        /// </summary>
        public SSRController pSSRController = new SSRController();

        /// <summary>
        ///		NF 控制器
        /// </summary>
        public NFController pNFController = new NFController();

        /// <summary>
        ///		启动
        /// </summary>
        /// <param name="server">服务器</param>
        /// <param name="mode">模式</param>
        /// <returns>是否启动成功</returns>
        public bool Start(Objects.Server server, Objects.Mode mode)
        {
            var result = false;
            switch (server.Type)
            {
                case "Socks5":
                    result = true;
                    break;
                case "Shadowsocks":
                    result = pSSController.Start(server);
                    break;
                case "ShadowsocksR":
                    result = pSSRController.Start(server);
                    break;
                case "VMess":
                    break;
                default:
                    break;
            }

            if (result)
            {
                result = pNFController.Start(server, mode);
            }

            if (!result)
            {
                Stop();
            }

            return result;
        }

        /// <summary>
        ///		停止
        /// </summary>
        public void Stop()
        {
            pNFController.Stop();
            pSSController.Stop();
            pSSRController.Stop();
        }
    }
}
