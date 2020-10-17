using Netch.Models;

namespace Netch.Controllers
{
    public interface IModeController : IController
    {
        /// <summary>
        ///     启动
        /// </summary>
        /// <param name="mode">模式</param>
        /// <returns>是否成功</returns>
        public abstract bool Start(in Mode mode);
    }
}