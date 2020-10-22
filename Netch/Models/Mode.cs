using System.Collections.Generic;
using System.Linq;
using System.Text;
using Netch.Utils;

namespace Netch.Models
{
    public class Mode
    {
        /// <summary>
        ///		备注
        /// </summary>
        public string Remark;

        /// <summary>
        ///     文件相对路径(必须是存在的文件)
        /// </summary>
        public string RelativePath;

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

        public bool SupportSocks5Auth => Type switch
        {
            0 => true,
            _ => false
        };

        public bool TestNatRequired => Type switch
        {
            0 => true,
            1 => true,
            2 => true,
            _ => false
        };

        /// <summary>
        ///    绕过中国（0. 不绕过 1. 绕过）
        /// </summary>
        public bool BypassChina = false;

        /// <summary>
        ///		规则
        /// </summary>
        public readonly List<string> Rule = new List<string>();

        public List<string> FullRule
        {
            get
            {
                var result = new List<string>();
                foreach (var s in Rule)
                {
                    if (string.IsNullOrWhiteSpace(s))
                        continue;
                    if (s.StartsWith("//"))
                        continue;

                    if (s.StartsWith("#include"))
                    {
                        var relativePath = new StringBuilder(s.Substring(8).Trim());
                        relativePath.Replace("<", "");
                        relativePath.Replace(">", "");
                        relativePath.Replace(".h", ".txt");

                        var mode = Global.Modes.FirstOrDefault(m => m.RelativePath.Equals(relativePath.ToString()));

                        if (mode == null)
                        {
                            Logging.Warning($"{relativePath} file included in {Remark} not found");
                        }
                        else if (mode == this)
                        {
                            Logging.Warning("Can't self-reference");
                        }
                        else
                        {
                            if (mode.Type != Type)
                            {
                                Logging.Warning($"{mode.Remark}'s mode is not as same as {Remark}'s mode");
                            }
                            else
                            {
                                if (mode.Rule.Any(rule => rule.StartsWith("#include")))
                                {
                                    Logging.Warning("Cannot reference mode that reference other mode");
                                }
                                else
                                {
                                    result.AddRange(mode.FullRule);
                                }
                            }
                        }
                    }
                    else
                    {
                        result.Add(s);
                    }
                }

                return result;
            }
        }


        /// <summary>
        ///		获取备注
        /// </summary>
        /// <returns>备注</returns>
        public override string ToString()
        {
            return $"[{Type + 1}] {i18N.Translate(Remark)}";
        }

        /// <summary>
        ///		获取模式文件字符串
        /// </summary>
        /// <returns>模式文件字符串</returns>
        public string ToFileString()
        {
            StringBuilder fileString = new StringBuilder();

            switch (Type)
            {
                case 0:
                    // 进程模式
                    fileString.Append($"# {Remark}");
                    break;
                case 1:
                    // TUN/TAP 规则内 IP CIDR，无 Bypass China 设置
                    fileString.Append($"# {Remark}, {Type}, 0");
                    break;
                default:
                    fileString.Append($"# {Remark}, {Type}, {(BypassChina ? 1 : 0)}");
                    break;
            }

            if (Rule.Any())
            {
                fileString.Append(Global.EOF);
                fileString.Append(string.Join(Global.EOF, Rule));
            }

            return fileString.ToString();
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