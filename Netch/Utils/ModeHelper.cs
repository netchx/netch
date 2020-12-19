using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Netch.Controllers;
using Netch.Forms;
using Netch.Models;
using Netch.Servers.Shadowsocks;
using Netch.Servers.Socks5;

namespace Netch.Utils
{
    public static class ModeHelper
    {
        private const string MODE_DIR = "mode";

        public static readonly string ModeDirectory = Path.Combine(Global.NetchDir, $"{MODE_DIR}\\");

        public static string GetRelativePath(string fullName) => fullName.Substring(ModeDirectory.Length);
        public static string GetFullPath(string relativeName) => Path.Combine(ModeDirectory, relativeName);
        public static string GetFullPath(Mode mode) => Path.Combine(ModeDirectory, mode.RelativePath);

        /// <summary>
        ///     从模式文件夹读取模式并为 <see cref="Forms.MainForm.ModeComboBox"/> 绑定数据
        /// </summary>
        public static void Load()
        {
            Global.Modes.Clear();

            if (!Directory.Exists(MODE_DIR)) return;

            var stack = new Stack<string>();
            stack.Push(MODE_DIR);
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

            Sort();
        }

        private static void LoadModeFile(string fullName)
        {
            var mode = new Mode
            {
                FileName = Path.GetFileNameWithoutExtension(fullName),
                RelativePath = GetRelativePath(fullName)
            };

            var content = File.ReadAllLines(fullName);
            if (content.Length == 0) return;

            for (var i = 0; i < content.Length; i++)
            {
                var text = content[i].Trim();

                if (i == 0)
                {
                    if (text.First() != '#')
                        return;
                    try
                    {
                        var splited = text.Substring(1).Split(',').Select(s => s.Trim()).ToArray();

                        mode.Remark = splited[0];

                        var typeResult = int.TryParse(splited.ElementAtOrDefault(1), out var type);
                        mode.Type = typeResult ? type : 0;

                        var bypassChinaResult = int.TryParse(splited.ElementAtOrDefault(2), out var bypassChina);
                        mode.BypassChina = mode.ClientRouting() && bypassChinaResult && bypassChina == 1;
                    }
                    catch
                    {
                        return;
                    }
                }
                else
                {
                    mode.Rule.Add(text);
                }
            }

            Global.Modes.Add(mode);
        }

        public static void WriteFile(Mode mode)
        {
            if (!Directory.Exists(ModeDirectory))
            {
                Directory.CreateDirectory(ModeDirectory);
            }

            var fullName = GetFullPath(mode.RelativePath ?? mode.FileName + ".txt");

            if (mode.RelativePath == null && File.Exists(fullName))
            {
                throw new Exception("新建模式的文件名已存在，请贡献者检查代码");
            }

            // 写入到模式文件里
            File.WriteAllText(fullName, mode.ToFileString());
            mode.RelativePath = GetRelativePath(fullName);
        }

        private static void Sort()
        {
            Global.Modes.Sort((a, b) => string.Compare(a.Remark, b.Remark, StringComparison.Ordinal));
        }

        public static void Add(Mode mode)
        {
            Global.Modes.Add(mode);
            Sort();
            Global.MainForm.InitMode();
        }

        public static void Delete(Mode mode)
        {
            var fullName = GetFullPath(mode);
            if (File.Exists(fullName))
            {
                File.Delete(fullName);
            }

            Global.Modes.Remove(mode);
            Global.MainForm.InitMode();
        }


        public static bool SkipServerController(Server server, Mode mode)
        {
            return mode.Type switch
            {
                0 => server switch
                {
                    Socks5 => true,
                    Shadowsocks shadowsocks when !shadowsocks.HasPlugin() && Global.Settings.RedirectorSS => true,
                    _ => false
                },
                _ => false
            };
        }

        public static IModeController GetModeControllerByType(int type, out ushort? port, out string portName, out PortType portType)
        {
            IModeController modeController;
            port = null;
            portName = string.Empty;
            portType = PortType.Both;
            switch (type)
            {
                case 0:
                    modeController = new NFController();
                    port = Global.Settings.RedirectorTCPPort;
                    portName = "Redirector TCP";
                    break;
                case 1:
                case 2:
                    modeController = new TUNTAPController();
                    break;
                case 3:
                case 5:
                    modeController = new HTTPController();
                    port = Global.Settings.HTTPLocalPort;
                    portName = "HTTP";
                    MainForm.StatusPortInfoText.HttpPort = (ushort) port;
                    break;
                case 4:
                    modeController = null;
                    break;
                default:
                    Logging.Error("未知模式类型");
                    throw new StartFailedException();
            }

            return modeController;
        }
    }
}