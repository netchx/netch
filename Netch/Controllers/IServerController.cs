using Netch.Models;

namespace Netch.Controllers
{
    public interface IServerController : IController
    {
        public int? Socks5LocalPort { get; set; }

        public string LocalAddress { get; set; }

        /// <summary>
        ///     启动
        /// </summary>
        /// <param name="server">服务器</param>
        /// <param name="mode">模式</param>
        /// <returns>是否启动成功</returns>
        public abstract bool Start(Server server, Mode mode);
    }
}