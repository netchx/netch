using Netch.Models;

namespace Netch.Interfaces
{
    public interface IModeController : IController
    {
        /// <summary>
        ///     启动
        /// </summary>
        /// <param name="mode">模式</param>
        /// <returns>是否成功</returns>
        public abstract void Start(in Mode mode);
    }
}