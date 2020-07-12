using System.Diagnostics;
using Netch.Models;

namespace Netch.Controllers
{
    public abstract class EncryptedProxy : Controller
    {
        /// <summary>
        ///     启动
        /// </summary>
        /// <param name="server">服务器</param>
        /// <param name="mode">模式</param>
        /// <returns>是否启动成功</returns>
        public abstract bool Start(Server server, Mode mode);

        public abstract void OnOutputDataReceived(object sender, DataReceivedEventArgs e);
    }
}