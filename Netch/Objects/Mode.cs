using System;
using System.Collections.Generic;
using System.IO;

namespace Netch.Objects
{
    public class Mode : IComparable<Mode>
    {
        /// <summary>
        ///		备注
        /// </summary>
        public string Remark;

        /// <summary>
        ///		文件名
        /// </summary>
        public string FileName;

        /// <summary>
        ///		文件SHA256的值
        /// </summary>
        public byte[] SHA256;

        /// <summary>
        ///		文件上次的写入时间
        /// </summary>
        public DateTime LastWriteTime;

        /// <summary>
        ///     类型（0. 进程加速 1. TUN/TAP IP 加速 2. TUN/TAP 全局代理绕过 IP 地址 3. 系统代理 1000. 只启动 Socks5 代理）
        /// </summary>
        public int Type = 0;

        /// <summary>
        ///    绕过中国（false. 不绕过 true. 绕过）
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
            return String.Format($"[{Type + 1}] {Remark} - {FileName}");
        }

        /// <summary>
        ///		从文件中创建模式
        /// </summary>
        public Mode(FileStream fs, out bool IsOk)
        {
            FileName = Path.GetFileName(fs.Name);
            var i = 0;
            string text;

            var sr = new StreamReader(fs);
            var str = new StringReader(sr.ReadToEnd());

            while ((text = str.ReadLine()) != null)
            {
                // 处理首行
                if (i == 0)
                {
                    // 将首行的内容除掉第一个字符以外，用逗号分隔开，并去掉空格
                    var splited = text.Trim().Substring(1).Split(',');

                    // 首行格式有误，首行为空格或者长度为 0
                    if (splited.Length == 0)
                    {
                        IsOk = false;
                        return;
                    }

                    // 首行如果有一个以上的内容，将第一个内容记录为备注
                    if (splited.Length >= 1)
                    {
                        Remark = splited[0].Trim();
                    }

                    // 首行如果有2个以上的内容
                    if (splited.Length >= 2)
                    {
                        // 如果第二个内容是 int 型，将其记录为模式类型
                        if (int.TryParse(splited[1], out int result))
                        {
                            Type = result;
                        }
                        // 否则为格式有误
                        else
                        {
                            IsOk = false;
                            return;
                        }
                    }

                    // 首行如果有3个以上的内容
                    if (splited.Length >= 3)
                    {
                        // 如果第三个内容是 int 型，将其记录为是否绕过中国类型（1 为是，其他为否）
                        if (int.TryParse(splited[2], out int result))
                        {
                            BypassChina = (result == 1);
                        }
                        // 否则为格式有误
                        else
                        {
                            IsOk = false;
                            return;
                        }
                    }
                }
                // 处理其他行
                else
                {
                    // 只要不是注释，且不为空，将其加入模式中的规则部分
                    if (!text.StartsWith("#") && !String.IsNullOrWhiteSpace(text))
                    {
                        Rule.Add(text.Trim());
                    }
                }

                // 行数统计
                i++;
            }
            System.Security.Cryptography.SHA256 modeSHA256 = System.Security.Cryptography.SHA256.Create();
            SHA256 = modeSHA256.ComputeHash(fs);
            FileInfo ModeFI = new FileInfo(fs.Name);
            LastWriteTime = ModeFI.LastWriteTime;
            IsOk = true;
        }

        /// <summary>
        ///		从 Process 窗口中创建模式
        /// </summary>
        public Mode()
        {
            // 跳过
        }

        /// <summary>
        ///	    CompareTo 方法
        /// </summary>
        public int CompareTo(Mode m)
        {
            return String.Compare(Remark, m.Remark, StringComparison.Ordinal);
        }
    }
}
