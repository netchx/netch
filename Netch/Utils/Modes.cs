using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Netch.Models;

namespace Netch.Utils
{
    public static class Modes
    {
        private static readonly string ModeDirectory = Path.Combine(Global.NetchDir, "mode\\");

        public static void Load()
        {
            Global.Modes.Clear();
            if (!Directory.Exists("mode")) return;

            var stack = new Stack<string>();
            stack.Push("mode");
            while (stack.Count > 0)
            {
                var dirInfo = new DirectoryInfo(stack.Pop());
                try
                {
                    foreach (var childDirInfo in dirInfo.GetDirectories())
                        stack.Push(childDirInfo.FullName);

                    foreach (var childFileInfo in dirInfo.GetFiles().Where(info => info.Name.EndsWith(".txt")))
                        LoadModeFile(childFileInfo.FullName);
                }
                catch
                {
                    // ignored
                }
            }

            Global.Modes.Sort((a, b) => string.Compare(a.Remark, b.Remark, StringComparison.Ordinal));
        }

        private static void LoadModeFile(string path)
        {
            var mode = new Mode
            {
                FileName = Path.GetFileNameWithoutExtension(path),
                RelativePath = path.Substring(ModeDirectory.Length)
            };

            var content = File.ReadAllLines(path);
            if (content.Length == 0) return;

            for (var i = 0; i < content.Length; i++)
            {
                var text = content[i];

                if (i == 0)
                {
                    var splited = text.Substring(text.IndexOf('#') + 1).Split(',').Select(s => s.Trim()).ToArray();
                    try
                    {
                        string tmp;
                        if ((tmp = splited.ElementAtOrDefault(0)) != null)
                            mode.Remark = i18N.Translate(tmp);

                        if ((tmp = splited.ElementAtOrDefault(1)) != null)
                            mode.Type = int.Parse(tmp);

                        if ((tmp = splited.ElementAtOrDefault(2)) != null)
                            mode.BypassChina = int.Parse(tmp) == 1;
                    }
                    catch
                    {
                        return;
                    }
                }
                else
                {
                    if (!text.StartsWith("#") && !string.IsNullOrWhiteSpace(text))
                        mode.Rule.Add(text.Trim());
                }
            }

            Global.Modes.Add(mode);
        }
    }
}