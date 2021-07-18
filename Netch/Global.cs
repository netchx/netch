using System;
using System.Collections.Generic;
using System.IO;

namespace Netch
{
    public static class Global
    {
        /// <summary>
        ///     版本号
        /// </summary>
        public static readonly string VerCode = "2.0.0";

        /// <summary>
        ///     日志记录
        /// </summary>
        public static Tools.Logger Logger = new() { SavePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Netch.log") };

        /// <summary>
        ///      配置文件
        /// </summary>
        public static Models.Config.Config Config;

        /// <summary>
        ///     节点列表
        /// </summary>
        public static List<Models.Server.ServerList> NodeList;

        /// <summary>
        ///     模式列表
        /// </summary>
        public static List<Models.Mode.Mode> ModeList;
    }
}
