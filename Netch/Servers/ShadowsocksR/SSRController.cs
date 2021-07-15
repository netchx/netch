using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Netch.Controllers;
using Netch.Interfaces;
using Netch.Models;

namespace Netch.Servers.ShadowsocksR
{
    public class SSRController : Guard, IServerController
    {
        public SSRController() : base("ShadowsocksR.exe")
        {
        }

        protected override IEnumerable<string> StartedKeywords => new[] { "listening at" };

        protected override IEnumerable<string> FailedKeywords => new[] { "Invalid config path", "usage" };

        public override string Name => "ShadowsocksR";

        public ushort? Socks5LocalPort { get; set; }

        public string? LocalAddress { get; set; }

        public async Task<Socks5> StartAsync(Server s)
        {
            var server = (ShadowsocksR)s;

            var command = new SSRParameter
            {
                s = await server.AutoResolveHostnameAsync(),
                p = server.Port,
                k = server.Password,
                m = server.EncryptMethod,
                t = "120",
                O = server.Protocol,
                G = server.ProtocolParam,
                o = server.OBFS,
                g = server.OBFSParam,
                b = this.LocalAddress(),
                l = this.Socks5LocalPort(),
                u = true
            };

            await StartGuardAsync(command.ToString());
            return new Socks5Bridge(IPAddress.Loopback.ToString(), this.Socks5LocalPort(),server.Hostname);
        }

        [Verb]
        class SSRParameter : ParameterBase
        {
            public string? s { get; set; }

            public ushort? p { get; set; }

            [Quote]
            public string? k { get; set; }

            public string? m { get; set; }

            public string? t { get; set; }

            [Optional]
            public string? O { get; set; }

            [Optional]
            public string? G { get; set; }

            [Optional]
            public string? o { get; set; }

            [Optional]
            public string? g { get; set; }

            public string? b { get; set; }

            public ushort? l { get; set; }

            public bool u { get; set; }

            [Full]
            [Quote]
            [Optional]
            public string? acl { get; set; }
        }
    }
}