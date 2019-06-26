using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Netch.Objects.SSD
{
    public class Main
    {
        /// <summary>
        ///     机场名
        /// </summary>
        public string airport;

        /// <summary>
        ///     端口
        /// </summary>
        public int port;

        /// <summary>
        ///     加密方式
        /// </summary>
        public string encryption;

        /// <summary>
        ///     密码
        /// </summary>
        public string password;

        /// <summary>
        ///     服务器数组
        /// </summary>
        public List<Server> servers;
    }
}
