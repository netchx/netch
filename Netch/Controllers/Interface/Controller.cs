using Netch.Models;

namespace Netch.Controllers
{
    public abstract partial class Controller
    {
        /// <summary>
        ///     控制器名
        /// </summary>
        public string Name { get; protected set; }

        /// <summary>
        ///     当前状态
        /// </summary>
        public State State { get; set; } = State.Waiting;

        /// <summary>
        ///     停止
        /// </summary>
        public abstract void Stop();
    }
}