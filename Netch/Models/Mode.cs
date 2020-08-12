using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Netch.Models
{
    public class Mode
    {
        /// <summary>
        ///		备注
        /// </summary>
        public string Remark;

        /// <summary>
        ///		无后缀文件名
        /// </summary>
        public string FileName;

        /// <summary>
        ///     类型<para />
        ///     0. Socks5 + 进程加速<para />
        ///     1. Socks5 + TUN/TAP 规则内 IP CIDR 加速<para />
        ///     2. Socks5 + TUN/TAP 全局，绕过规则内 IP CIDR<para />
        ///     3. Socks5 + HTTP 代理（设置到系统代理）<para />
        ///     4. Socks5 代理（不设置到系统代理）<para />
        ///     5. Socks5 + HTTP 代理（不设置到系统代理）<para />
        /// </summary>
        public int Type = 0;

        /// <summary>
        ///    绕过中国（0. 不绕过 1. 绕过）
        /// </summary>
        public bool BypassChina = false;

        /// <summary>
        ///		规则
        /// </summary>
        public readonly List<string> Rule = new List<string>();

        /// <summary>
        ///		获取备注
        /// </summary>
        /// <returns>备注</returns>
        public override string ToString()
        {
            return $"[{Type + 1}] {Remark}";
        }

        /// <summary>
        ///		获取模式文件字符串
        /// </summary>
        /// <returns>模式文件字符串</returns>
        public string ToFileString()
        {
            string fileString;

            switch (Type)
            {
                case 0:
                    // 进程模式
                    fileString = $"# {Remark}";
                    break;
                case 1:
                    // TUN/TAP 规则内 IP CIDR，无 Bypass China 设置
                    fileString = $"# {Remark}, {Type}, 0";
                    break;
                default:
                    fileString = $"# {Remark}, {Type}, {(BypassChina ? 1 : 0)}";
                    break;
            }

            fileString += Global.EOF;

            fileString = Rule.Aggregate(fileString, (current, item) => $"{current}{item}{Global.EOF}");
            // 去除最后的行尾符
            fileString = fileString.Substring(0, fileString.Length - 2);

            return fileString;
        }

        /// <summary>
        ///		写入模式文件
        /// </summary>
        public void ToFile(string Dir)
        {
            if (!System.IO.Directory.Exists(Dir))
            {
                System.IO.Directory.CreateDirectory(Dir);
            }

            var NewPath = System.IO.Path.Combine(Dir, FileName);
            if (System.IO.File.Exists(NewPath + ".txt"))
            {
                // 重命名该模式文件名
                NewPath += "_";

                while (System.IO.File.Exists(NewPath + ".txt"))
                {
                    // 循环重命名该模式文件名，直至不重名
                    NewPath += "_";
                }
            }

            FileName = System.IO.Path.GetFileName(NewPath);

            // 加上文件名后缀
            NewPath += ".txt";

            // 写入到模式文件里
            System.IO.File.WriteAllText(NewPath, ToFileString());
        }

        /// <summary>
        ///		删除模式文件
        /// </summary>
        public void DeleteFile(string Dir)
        {
            if (System.IO.Directory.Exists(Dir))
            {
                var NewPath = System.IO.Path.Combine(Dir, FileName);
                if (System.IO.File.Exists(NewPath + ".txt"))
                {
                    System.IO.File.Delete(NewPath + ".txt");
                }
            }
        }

        public string TypeToString()
        {
            return Type switch
            {
                0 => "Process",
                1 => "TUNTAP",
                2 => "TUNTAP",
                3 => "SYSTEM",
                4 => "S5",
                5 => "S5+HTTP",
                _ => "ERROR",
            };
        }
    }
}