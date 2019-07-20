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
        ///     类型
        ///     0. 进程加速
        ///     1. TUN/TAP 规则内 IP CIDR 加速
        ///     2. TUN/TAP 全局，绕过规则内 IP CIDR
        ///     3. HTTP 代理（自动设置到系统代理）
        ///     4. Socks5 代理（不自动设置到系统代理）
        ///     5. Socks5 + HTTP 代理（不自动设置到系统代理）
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
