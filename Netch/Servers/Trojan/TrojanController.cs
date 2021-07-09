using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.Json;
using Netch.Controllers;
using Netch.Interfaces;
using Netch.Models;
using Netch.Servers.Models;
using Netch.Utils;

namespace Netch.Servers
{
    public class TrojanController : Guard, IServerController
    {
        public TrojanController() : base("Trojan.exe")
        {
        }

        protected override IEnumerable<string> StartedKeywords => new[] { "started" };

        protected override IEnumerable<string> FailedKeywords => new[] { "exiting" };

        public override string Name => "Trojan";

        public ushort? Socks5LocalPort { get; set; }

        public string? LocalAddress { get; set; }

        public Socks5 Start(in Server s)
        {
            var server = (Trojan)s;
            var trojanConfig = new TrojanConfig
            {
                local_addr = this.LocalAddress(),
                local_port = this.Socks5LocalPort(),
                remote_addr = server.AutoResolveHostname(),
                remote_port = server.Port,
                password = new List<string>
                {
                    server.Password
                },
                ssl = new TrojanSSL
                {
                    sni = server.Host.ValueOrDefault() ?? (Global.Settings.ResolveServerHostname ? server.Hostname : "")
                }
            };

            using (var fileStream = new FileStream(Constants.TempConfig, FileMode.Create, FileAccess.Write))
            {
                JsonSerializer.SerializeAsync(fileStream, trojanConfig, Global.NewCustomJsonSerializerOptions()).Wait();
            }

            StartGuard("-c ..\\data\\last.json");
            return new Socks5Bridge(IPAddress.Loopback.ToString(), this.Socks5LocalPort(), server.Hostname);
        }
    }
}