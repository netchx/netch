using System.Collections.Generic;
using System.Globalization;

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
            if (CultureInfo.CurrentCulture.Name == "zh-CN")
            {
                string Stype;
                if (Type == 0)
                {
                    Stype = "[进程模式] ";
                }
                else if (Type == 1)
                {
                    Stype = "[TUN/TAP 黑] ";
                }
                else if (Type == 2)
                {
                    Stype = "[TUN/TAP 白] ";
                }
                else
                {
                    Stype = "";
                }

                return string.Format("{0}{1}", Stype, Remark);
            }
            else
            {
                return string.Format("[{0}] {1}", Type + 1, Remark);
            }
        }

        /// <summary>
        ///		获取模式文件字符串
        /// </summary>
        /// <returns>模式文件字符串</returns>
        public string ToFileString()
        {
            string FileString;

            // 进程模式
            if (Type == 0)
            {
                FileString = $"# {Remark}\r\n";
            }

            // TUN/TAP 规则内 IP CIDR，无 Bypass China 设置
            else if (Type == 1)
            {
                FileString = $"# {Remark}, {Type}, 0\r\n";
            }

            // TUN/TAP 全局，绕过规则内 IP CIDR
            // HTTP 代理（自动设置到系统代理）
            // Socks5 代理（不自动设置到系统代理）
            // Socks5 + HTTP 代理（不自动设置到系统代理）
            else
            {
                FileString = $"# {Remark}, {Type}, {(BypassChina ? 1 : 0)}\r\n";
            }

            foreach (var item in Rule)
            {
                FileString = $"{FileString}{item}\r\n";
            }

            // 去除最后两个多余回车符和换行符
            FileString = FileString.Substring(0, FileString.Length - 2);

            return FileString;
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
    }
}
