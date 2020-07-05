namespace Netch.Models
{
    /// <summary>
    ///     状态
    /// </summary>
    public enum State
    {
        /// <summary>
        ///     等待命令中
        /// </summary>
        Waiting,

        /// <summary>
        ///     正在启动中
        /// </summary>
        Starting,

        /// <summary>
        ///     已启动
        /// </summary>
        Started,

        /// <summary>
        ///     正在停止中
        /// </summary>
        Stopping,

        /// <summary>
        ///     已停止
        /// </summary>
        Stopped,


        /// <summary>
        ///     退出中
        /// </summary>
        Terminating
    }

    public static class StateExtension
    {
        public static string GetStatusString(State state)
        {
            return state switch
            {
                State.Started => state.ToString(),
                State.Stopping => state.ToString(),
                State.Stopped => state.ToString(),
                State.Terminating => state.ToString(),
                State.Starting => state.ToString(),
                State.Waiting => "Waiting for command",
                _ => ""
            };
        }
    }
}