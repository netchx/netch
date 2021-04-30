using System.Collections.Generic;
using Netch.Controllers;
using Netch.Interfaces;
using Netch.Models;

namespace Netch.Servers.Shadowsocks
{
    public class SSController : Guard, IServerController
    {
        public override string MainFile { get; protected set; } = "Shadowsocks.exe";

        protected override IEnumerable<string> StartedKeywords { get; set; } = new[] { "listening at" };

        protected override IEnumerable<string> StoppedKeywords { get; set; } = new[] { "Invalid config path", "usage", "plugin service exit unexpectedly" };

        public override string Name { get; } = "Shadowsocks";

        public ushort? Socks5LocalPort { get; set; }

        public string? LocalAddress { get; set; }

        public void Start(in Server s, in Mode mode)
        {
            var server = (Shadowsocks)s;

            var command = new SSParameter
            {
                s = server.AutoResolveHostname(),
                p = server.Port,
                b = this.LocalAddress(),
                l = this.Socks5LocalPort(),
                m = server.EncryptMethod,
                k = server.Password,
                u = true,
                plugin = server.Plugin,
                plugin_opts = server.PluginOption
            };

            StartInstanceAuto(command.ToString());
        }

        [Verb]
        private class SSParameter : ParameterBase
        {
            public string? s { get; set; }

            public ushort? p { get; set; }

            public string? b { get; set; }

            public ushort? l { get; set; }

            public string? m { get; set; }

            public string? k { get; set; }

            public bool u { get; set; }

            [Full]
            [Optional]
            public string? plugin { get; set; }

            [Full]
            [Optional]
            [RealName("plugin-opts")]
            public string? plugin_opts { get; set; }

            [Full]
            [Quote]
            [Optional]
            public string? acl { get; set; }
        }

        public override void Stop()
        {
            StopInstance();
        }
    }
}