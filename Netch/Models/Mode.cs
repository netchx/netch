using Netch.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Netch.Models
{
    public class Mode
    {
        private readonly Lazy<List<string>> _lazyRule;

        public string? FullName { get; private set; }

        public Mode(string? fullName)
        {
            _lazyRule = new Lazy<List<string>>(ReadRules);
            if (fullName == null)
                return;

            FullName = fullName;
            if (!File.Exists(FullName))
                return;

            var text = File.ReadLines(FullName).First();

            // load head
            if (text.First() != '#')
                throw new Exception($"mode {FullName} head not found at Line 0");

            var split = text.Substring(1).SplitTrimEntries(',');
            Remark = split[0];

            var typeResult = int.TryParse(split.ElementAtOrDefault(1), out var type);
            Type = typeResult ? type : 0;
            if (!ModeHelper.ModeTypes.Contains(Type))
                throw new NotSupportedException($"not support mode \"[{Type}]{Remark}\".");
        }

        /// <summary>
        ///     规则
        /// </summary>
        public List<string> Rule => _lazyRule.Value;

        /// <summary>
        ///     备注
        /// </summary>
        public string Remark { get; set; } = "";

        /// <summary>
        ///     类型
        ///     <para />
        ///     0. Socks5 + 进程加速
        ///     <para />
        ///     1. Socks5 + TUN/TAP 规则内 IP CIDR 加速
        ///     <para />
        ///     2. Socks5 + TUN/TAP 全局，绕过规则内 IP CIDR
        ///     <para />
        ///     3. Socks5 + HTTP 代理（设置到系统代理）
        ///     <para />
        ///     4. Socks5 代理（不设置到系统代理）
        ///     <para />
        ///     5. Socks5 + HTTP 代理（不设置到系统代理）
        ///     <para />
        /// </summary>
        public int Type { get; set; } = 0;

        /// <summary>
        ///     文件相对路径(必须是存在的文件)
        /// </summary>
        public string? RelativePath => FullName == null ? null : ModeHelper.GetRelativePath(FullName);

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

                        var mode = Global.Modes.FirstOrDefault(m => m.FullName != null && m.RelativePath!.Equals(relativePath.ToString()));

                        if (mode == null)
                            throw new MessageException($"{relativePath} file included in {Remark} not found");

                        if (mode == this)
                            throw new MessageException("Can't self-reference");

                        if (mode.Type != Type)
                            throw new MessageException($"{mode.Remark}'s mode is not as same as {Remark}'s mode");

                        if (mode.Rule.Any(rule => rule.StartsWith("#include")))
                            throw new Exception("Cannot reference mode that reference other mode");

                        result.AddRange(mode.FullRule);
                    }
                    else
                    {
                        result.Add(s);
                    }
                }

                return result;
            }
        }

        private List<string> ReadRules()
        {
            if (FullName == null || !File.Exists(FullName))
                return new List<string>();

            return File.ReadLines(FullName!).Skip(1).ToList();
        }

        public void WriteFile(string? fullName = null)
        {
            if (fullName != null)
                throw new NotImplementedException();

            var dir = Path.GetDirectoryName(FullName)!;
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            // 写入到模式文件里
            File.WriteAllText(FullName!, ToFileString());
        }

        /// <summary>
        ///     获取备注
        /// </summary>
        /// <returns>备注</returns>
        public override string ToString()
        {
            return $"[{Type + 1}] {i18N.Translate(Remark)}";
        }

        /// <summary>
        ///     获取模式文件字符串
        /// </summary>
        /// <returns>模式文件字符串</returns>
        public string ToFileString()
        {
            return $"# {Remark}, {Type}{Constants.EOF}{string.Join(Constants.EOF, Rule)}";
        }
    }

    public static class ModeExtension
    {
        /// 是否会转发 UDP
        public static bool TestNatRequired(this Mode mode)
        {
            return mode.Type is 0 or 2;
        }
    }
}