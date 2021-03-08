using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Netch.Controllers;

namespace Netch.Utils.HttpProxyHandler
{
    /// <summary>
    ///     提供PAC功能支持
    /// </summary>
    internal static class PACServerHandle
    {
        private static HttpWebServer? _httpWebServer;
        private static string? _pacContent;
        public static readonly string PacPrefix= $"http://127.0.0.1:{Global.Settings.Pac_Port}/pac/";

        public static string InitPACServer()
        {
            try
            {
                _pacContent = GetPacList("127.0.0.1");

                _httpWebServer = new HttpWebServer(SendResponse, PacPrefix);
                Task.Run(() => _httpWebServer.StartWaitingRequest());

                var pacUrl = GetPacUrl();
                Logging.Info($"Webserver InitServer OK: {pacUrl}");
                return pacUrl;
            }
            catch
            {
                Logging.Error("Webserver InitServer Failed");
                throw;
            }
        }

        public static string SendResponse(HttpListenerRequest request)
        {
            return _pacContent!;
        }

        public static void Stop()
        {
            try
            {
                _httpWebServer?.Stop();
            }
            catch
            {
                // ignored
            }

            _httpWebServer = null;
        }

        private static string GetPacList(string address)
        {
            try
            {
                var proxy = $"PROXY {address}:{Global.Settings.HTTPLocalPort};";
                var pacfile = Path.Combine(Global.NetchDir, "bin\\pac.txt");

                var pac = File.ReadAllText(pacfile, Encoding.UTF8).Replace("__PROXY__", proxy);
                return pac;
            }
            catch
            {
                throw new MessageException("Pac file not found!");
            }
        }

        /// <summary>
        ///     获取PAC地址
        /// </summary>
        /// <returns></returns>
        public static string GetPacUrl()
        {
            return PacPrefix + $"?t={DateTime.Now:yyyyMMddHHmmssfff}";
        }
    }
}