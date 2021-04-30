using System.Collections.Generic;
using Netch.Controllers;
using Netch.Interfaces;
using Netch.Models;

namespace Netch.Servers.ShadowsocksR
{
    public class SSRController : Guard, IServerController
    {
        public override string MainFile { get; protected set; } = "ShadowsocksR.exe";

        protected override IEnumerable<string> StartedKeywords { get; set; } = new[] { "listening at" };

        protected override IEnumerable<string> StoppedKeywords { get; set; } = new[] { "Invalid config path", "usage" };

        public override string Name { get; } = "ShadowsocksR";

        public ushort? Socks5LocalPort { get; set; }

        public string? LocalAddress { get; set; }

        public void Start(in Server s, in Mode mode)
        {
            var server = (ShadowsocksR)s;

            var command = new SSRParameter
            {
                s = server.AutoResolveHostname(),
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

            StartInstanceAuto(command.ToString());
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

        public override void Stop()
        {
            StopInstance();
        }
    }
}