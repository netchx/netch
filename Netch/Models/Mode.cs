using Netch.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Netch.Enums;

namespace Netch.Models
{
    public class Mode
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="fullName">Mode File FullPath</param>
        /// <exception cref="FormatException"></exception>
        /// <exception cref="NotSupportedException"></exception>
        public Mode(string? fullName)
        {
            FullName = fullName;
            if (FullName == null || !File.Exists(FullName))
                return;

            (Remark, Type) = ReadHead(FullName);
        }

        public string? FullName { get; }

        /// <summary>
        ///     规则
        /// </summary>
        public List<string> Content => _content ??= ReadContent();

        private List<string>? _content;

        /// <summary>
        ///     备注
        /// </summary>
        public string Remark { get; set; } = "";

        public ModeType Type { get; set; } = ModeType.Process;

        /// <summary>
        ///     文件相对路径(必须是存在的文件)
        /// </summary>
        public string? RelativePath => FullName == null ? null : ModeHelper.GetRelativePath(FullName);

        public IEnumerable<string> GetRules()
        {
            var result = new List<string>();
            foreach (var s in Content)
            {
                if (string.IsNullOrWhiteSpace(s))
                    continue;

                if (s.StartsWith("//"))
                    continue;

                const string include = "#include";
                if (s.StartsWith(include))
                {
                    var relativePath = new StringBuilder(s[include.Length..].Trim());
                    relativePath.Replace("<", "").Replace(">", "");
                    relativePath.Replace(".h", ".txt");

                    var mode = Global.Modes.FirstOrDefault(m => m.RelativePath?.Equals(relativePath.ToString()) ?? false) ??
                               throw new MessageException($"{relativePath} file included in {Remark} not found");

                    if (mode == this)
                        throw new MessageException("Can't self-reference");

                    if (mode.Type != Type)
                        throw new MessageException($"{mode.Remark}'s mode is not as same as {Remark}'s mode");

                    if (mode.Content.Any(rule => rule.StartsWith(include)))
                        throw new Exception("Cannot reference mode that reference other mode");

                    result.AddRange(mode.GetRules());
                }
                else
                {
                    result.Add(s);
                }
            }

            return result;
        }

        private static (string, ModeType) ReadHead(string fileName)
        {
            var text = File.ReadLines(fileName).First();
            if (text.First() != '#')
                throw new FormatException($"{fileName} head not found at Line 0");

            var split = text[1..].SplitTrimEntries(',');

            var typeNumber = int.TryParse(split.ElementAtOrDefault(1), out var type) ? type : 0;
            if (!Enum.GetValues(typeof(ModeType)).Cast<int>().Contains(typeNumber))
                throw new NotSupportedException($"Not support mode \"{typeNumber}\".");

            return (split[0], (ModeType)typeNumber);
        }

        private List<string> ReadContent()
        {
            if (FullName == null || !File.Exists(FullName))
                return new List<string>();

            return File.ReadLines(FullName).Skip(1).ToList();
        }

        public void ResetContent()
        {
            _content = null;
        }

        public void WriteFile()
        {
            var dir = Path.GetDirectoryName(FullName)!;
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var content = $"# {Remark}, {(int)Type}{Constants.EOF}{string.Join(Constants.EOF, Content)}";
            // 写入到模式文件里
            File.WriteAllText(FullName!, content);
        }

        /// <summary>
        ///     获取备注
        /// </summary>
        /// <returns>备注</returns>
        public override string ToString()
        {
            return $"[{(int)Type + 1}] {i18N.Translate(Remark)}";
        }
    }

    public static class ModeExtension
    {
        /// 是否会转发 UDP
        public static bool TestNatRequired(this Mode mode)
        {
            return mode.Type is ModeType.Process or ModeType.BypassRuleIPs;
        }
    }
}