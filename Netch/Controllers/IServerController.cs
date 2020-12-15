using Netch.Models;

namespace Netch.Controllers
{
    public interface IServerController : IController
    {
        public ushort? Socks5LocalPort { get; set; }

        public string LocalAddress { get; set; }

        /// <summary>
        ///     启动
        /// </summary>
        /// <param name="s">服务器</param>
        /// <param name="mode">模式</param>
        /// <returns>是否启动成功</returns>
        public abstract bool Start(in Server s, in Mode mode);
    }

    public static class ServerControllerExtension
    {
        public static ushort Socks5LocalPort(this IServerController controller)
        {
            return controller.Socks5LocalPort ?? Global.Settings.Socks5LocalPort;
        }

        public static string LocalAddress(this IServerController controller)
        {
            return controller.LocalAddress ?? Global.Settings.LocalAddress;
        }
    }
}