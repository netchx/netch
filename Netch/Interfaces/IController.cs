namespace Netch.Interfaces
{
    public interface IController
    {
        /// <summary>
        ///     控制器名
        /// </summary>
        public string Name { get; }

        /// <summary>
        ///     停止
        /// </summary>
        public void Stop();
    }
}