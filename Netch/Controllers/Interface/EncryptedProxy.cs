using Netch.Models;

namespace Netch.Controllers
{
    public abstract class EncryptedProxy : Controller
    {
        private int? _socks5Port;

        public int Socks5LocalPort
        {
            get => _socks5Port ?? Global.Settings.Socks5LocalPort;
            set => _socks5Port = value;
        }

        private string _localAddress;

        public string LocalAddress
        {
            get => _localAddress ?? Global.Settings.LocalAddress;
            set => _localAddress = value;
        }

        /// <summary>
        ///     启动
        /// </summary>
        /// <param name="server">服务器</param>
        /// <param name="mode">模式</param>
        /// <returns>是否启动成功</returns>
        public abstract bool Start(Server server, Mode mode);
    }
}