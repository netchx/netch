using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Netch.Enums;
using Netch.Utils;

namespace Netch.Models
{
    public class Mode
    {
        private List<string>? _content;

        public Mode(string? fullName)
        {
            FullName = fullName;
            if (FullName == null || !File.Exists(FullName))
                return;

            Load();
        }

        public string? FullName { get; }

        public List<string> Content => _content ??= ReadContent();

        public string Remark { get; set; } = "";

        public ModeType Type { get; set; } = ModeType.Process;

        public string? RelativePath => FullName == null ? null : ModeHelper.GetRelativePath(FullName);

        private void Load()
        {
            if (FullName == null)
                return;

            (Remark, Type) = ReadHead(FullName);
            _content = null;
        }

        public IEnumerable<string> GetRules()
        {
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

                    foreach (var rule in mode.GetRules())
                        yield return rule;
                }
                else
                {
                    yield return s;
                }
            }
        }

        private static (string, ModeType) ReadHead(string fileName)
        {
            var text = File.ReadLines(fileName).First();
            if (text.First() != '#')
                throw new FormatException($"{fileName} head not found at Line 0");

            var strings = text[1..].SplitTrimEntries(',');

            var remark = strings[0];
            var typeNumber = int.TryParse(strings.ElementAtOrDefault(1), out var type) ? type : 0;

            if (!Enum.GetValues(typeof(ModeType)).Cast<int>().Contains(typeNumber))
                throw new NotSupportedException($"Not support mode \"{typeNumber}\".");

            return (remark, (ModeType)typeNumber);
        }

        private List<string> ReadContent()
        {
            if (FullName == null || !File.Exists(FullName))
                return new List<string>();

            return File.ReadLines(FullName).Skip(1).ToList();
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
            return mode.Type is ModeType.Process && Global.Settings.Redirector.FilterProtocol.HasFlag(PortType.UDP) ||
                   mode.Type is ModeType.BypassRuleIPs;
        }
    }
}