namespace Netch.Models.Server.VLess
{
    public class VLess : Server
    {
        public VLess()
        {
            this.Type = ServerType.VLess;
        }

        /// <summary>
        ///     自定义配置
        /// </summary>
        public bool Custom = true;

        /// <summary>
        ///     自定义配置文件路径
        /// </summary>
        public string FilePath;

        /// <summary>
        ///     解析链接
        /// </summary>
        /// <param name="link">链接</param>
        /// <returns>是否成功</returns>
        public bool ParseLink(string link)
        {
            return false;
        }
    }
}
