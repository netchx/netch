using System.Threading.Tasks;
using WindowsProxy;
using Netch.Models;
using Netch.Servers.Socks5;
using Netch.Servers.Trojan;
using Netch.Utils;
using Netch.Utils.HttpProxyHandler;

namespace Netch.Controllers
{
    public class HTTPController : IModeController
    {
        public readonly PrivoxyController PrivoxyController = new();

        private ProxyStatus? _oldState;

        public string Name { get; } = "HTTP";

        /// <summary>
        ///     启动
        /// </summary>
        /// <param name="mode">模式</param>
        /// <returns>是否启动成功</returns>
        public void Start(in Mode mode)
        {
            PrivoxyController.Start(MainController.Server!);
            Global.Job.AddProcess(PrivoxyController.Instance!);
            string? pacUrl = null;

            if (MainController.Server is Socks5 or Trojan && mode.BypassChina || (Global.Settings.AlwaysStartPACServer ?? false))
            {
                try
                {
                    PortHelper.CheckPort(Global.Settings.Pac_Port);
                }
                catch
                {
                    Global.Settings.Pac_Port = PortHelper.GetAvailablePort();
                }

                pacUrl = PACServerHandle.InitPACServer();
            }

            if (mode.Type is 3)
            {
                using var service = new ProxyService();
                _oldState = service.Query();

                if (pacUrl != null)
                {
                    service.AutoConfigUrl = pacUrl;
                    service.Pac();
                }
                else
                {
                    service.Server = $"127.0.0.1:{Global.Settings.HTTPLocalPort}";
                    service.Bypass = string.Join(";", ProxyService.LanIp);

                    service.Global();
                }
            }
        }

        /// <summary>
        ///     停止
        /// </summary>
        public void Stop()
        {
            var tasks = new[]
            {
                Task.Run(PrivoxyController.Stop),
                Task.Run(() =>
                {
                    PACServerHandle.Stop();

                    if (_oldState != null)
                    {
                        using var service = new ProxyService();
                        if (_oldState.IsProxy && _oldState.ProxyServer == service.Query().ProxyServer ||
                            _oldState.IsAutoProxyUrl && _oldState.AutoConfigUrl!.StartsWith(PACServerHandle.PacPrefix))
                        {
                            service.Direct();
                            return;
                        }

                        service.Set(_oldState);
                    }
                })
            };

            Task.WaitAll(tasks);
        }
    }
}