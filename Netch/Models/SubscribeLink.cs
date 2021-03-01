namespace Netch.Models
{
    public class SubscribeLink
    {
        /// <summary>
        ///     启用状态
        /// </summary>
        public bool Enable { get; set; } = true;

        /// <summary>
        ///     链接
        /// </summary>
        public string Link { get; set; } = string.Empty;

        /// <summary>
        ///     备注
        /// </summary>
        public string Remark { get; set; } = string.Empty;

        /// <summary>
        ///     User Agent
        /// </summary>
        public string UserAgent { get; set; } = string.Empty;
    }
}