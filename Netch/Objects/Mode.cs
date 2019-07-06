using System;
using System.Collections.Generic;

namespace Netch.Objects
{
    public class Mode
    {
        /// <summary>
        ///		备注
        /// </summary>
        public string Remark;

        /// <summary>
        ///     类型（0. 进程加速 1. TUN/TAP IP 加速 2. TUN/TAP 全局代理绕过 IP 地址 3. 系统代理 1000. 只启动 Socks5 代理）
        /// </summary>
        public int Type = 0;

        /// <summary>
        ///    绕过中国（0. 不绕过 1. 绕过）
        /// </summary>
        public bool BypassChina = false;

        /// <summary>
        ///		规则
        /// </summary>
        public List<string> Rule = new List<string>();

        /// <summary>
        ///		获取备注
        /// </summary>
        /// <returns>备注</returns>
        public override string ToString()
        {
            return String.Format("[{0}] {1}", Type + 1, Remark);
        }
    }
}
