using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Netch.Controllers;
using Netch.Interfaces;
using Netch.Models;

namespace Netch.Servers.Shadowsocks
{
    public class SSController : Guard, IServerController
    {
        public SSController() : base("Shadowsocks.exe")
        {
        }

        protected override IEnumerable<string> StartedKeywords => new[] { "listening at" };

        protected override IEnumerable<string> FailedKeywords => new[] { "Invalid config path", "usage", "plugin service exit unexpectedly" };

        public override string Name => "Shadowsocks";

        public ushort? Socks5LocalPort { get; set; }

        public string? LocalAddress { get; set; }

        public async Task<Socks5> StartAsync(Server s)
        {
            var server = (Shadowsocks)s;

            var command = new SSParameter
            {
                s = await server.AutoResolveHostnameAsync(),
                p = server.Port,
                b = this.LocalAddress(),
                l = this.Socks5LocalPort(),
                m = server.EncryptMethod,
                k = server.Password,
                u = true,
                plugin = server.Plugin,
                plugin_opts = server.PluginOption
            };

            await StartGuardAsync(command.ToString());
            return new Socks5Bridge(IPAddress.Loopback.ToString(), this.Socks5LocalPort(), server.Hostname);
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

            [Full] [Optional] public string? plugin { get; set; }

            [Full]
            [Optional]
            [RealName("plugin-opts")]
            public string? plugin_opts { get; set; }

            [Full] [Quote] [Optional] public string? acl { get; set; }
        }
    }
}