using System.Collections.Generic;
using System.IO;
using System.Text;
using Netch.Controllers;
using Netch.Models;

namespace Netch.Servers.Shadowsocks
{
    public class SSController : Guard, IServerController
    {
        public override string MainFile { get; protected set; } = "Shadowsocks.exe";

        protected override IEnumerable<string> StartedKeywords { get; } = new[] {"listening at"};

        protected override IEnumerable<string> StoppedKeywords { get; } = new[] {"Invalid config path", "usage", "plugin service exit unexpectedly"};

        public override string Name { get; } = "Shadowsocks";

        public ushort? Socks5LocalPort { get; set; }

        public string? LocalAddress { get; set; }

        public void Start(in Server s, in Mode mode)
        {
            var server = (Shadowsocks) s;

            #region Argument

            var argument = new StringBuilder();
            argument.Append($"-s {server.AutoResolveHostname()} " + $"-p {server.Port} " + $"-b {this.LocalAddress()} " +
                            $"-l {this.Socks5LocalPort()} " + $"-m {server.EncryptMethod} " + $"-k \"{server.Password}\" " + "-u");

            if (!string.IsNullOrWhiteSpace(server.Plugin) && !string.IsNullOrWhiteSpace(server.PluginOption))
                argument.Append($" --plugin {server.Plugin}" + $" --plugin-opts \"{server.PluginOption}\"");

            if (mode.BypassChina)
                argument.Append($" --acl \"{Path.GetFullPath(File.Exists(Global.UserACL) ? Global.UserACL : Global.BuiltinACL)}\"");

            #endregion

            StartInstanceAuto(argument.ToString());
        }

        public override void Stop()
        {
                StopInstance();
        }
    }
}