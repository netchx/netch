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

            if (mode.Type is 3)
            {
                using var service = new ProxyService();
                _oldState = service.Query();

                if (MainController.Server is Socks5 or Trojan && mode.BypassChina)
                {
                    service.AutoConfigUrl = PACServerHandle.InitPACServer("127.0.0.1");

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
                        service.Set(_oldState!);
                    }
                })
            };

            Task.WaitAll(tasks);
        }
    }
}