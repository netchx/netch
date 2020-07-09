using System.Diagnostics;
using Netch.Models;

namespace Netch.Controllers
{
    public abstract class ServerClient : Controller
    {
        /// <summary>
        ///     启动
        /// </summary>
        /// <param name="server">服务器</param>
        /// <param name="mode">模式</param>
        /// <returns>是否启动成功</returns>
        public abstract bool Start(Server server, Mode mode);

        /// <summary>
        ///     ServerClient 停止
        ///     <param />
        ///     注意 对象类型 以调用子类 Stop() 方法
        /// </summary>
        public new void Stop()
        {
            base.Stop();

            // SSController Stop()
            // 不能自动转换对象类型的兼容代码 :(
            if (Global.Settings.BootShadowsocksFromDLL) NativeMethods.Shadowsocks.Stop();
        }

        public abstract void OnOutputDataReceived(object sender, DataReceivedEventArgs e);
    }
}