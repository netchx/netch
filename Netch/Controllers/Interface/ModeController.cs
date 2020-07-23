using Netch.Models;

namespace Netch.Controllers
{
    public abstract class ModeController : Controller
    {
        /// <summary>
        ///     启动
        /// </summary>
        /// <param name="server">服务器</param>
        /// <param name="mode">模式</param>
        /// <returns>是否成功</returns>
        public abstract bool Start(Server server, Mode mode);
    }
}