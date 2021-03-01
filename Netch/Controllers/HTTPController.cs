using System.Threading.Tasks;
using WindowsProxy;
using Netch.Models;
using Netch.Servers.Socks5;
using Netch.Servers.Trojan;
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

            if (mode.Type is 3 or 5)
            {
                if (MainController.Server is Socks5 or Trojan && mode.BypassChina)
                {
                    PACServerHandle.InitPACServer("127.0.0.1");
                }
                else
                {
                    using var service = new ProxyService
                    {
                        Server = $"127.0.0.1:{Global.Settings.HTTPLocalPort}",
                        Bypass = string.Join(";", ProxyService.LanIp)
                    };

                    _oldState = service.Query();
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

                    using var service = new ProxyService();
                    service.Set(_oldState!);
                })
            };

            Task.WaitAll(tasks);
        }
    }
}