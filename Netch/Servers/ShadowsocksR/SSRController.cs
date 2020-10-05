using System.Text;
using Netch.Controllers;
using Netch.Models;
using Netch.Utils;

namespace Netch.Servers.ShadowsocksR
{
    public class SSRController : Guard, IServerController
    {
        public override string MainFile { get; protected set; } = "ShadowsocksR.exe";

        public override string Name { get; protected set; } = "ShadowsocksR";

        public int? Socks5LocalPort { get; set; }
        public string LocalAddress { get; set; }

        public bool Start(Server s, Mode mode)
        {
            var server = (ShadowsocksR) s;

            #region Argument

            var argument = new StringBuilder();
            argument.Append($"-s {DNS.Lookup(server.Hostname)} -p {server.Port} -k \"{server.Password}\" -m {server.EncryptMethod} -t 120");
            if (!string.IsNullOrEmpty(server.Protocol))
            {
                argument.Append($" -O {server.Protocol}");
                if (!string.IsNullOrEmpty(server.ProtocolParam)) argument.Append($" -G \"{server.ProtocolParam}\"");
            }

            if (!string.IsNullOrEmpty(server.OBFS))
            {
                argument.Append($" -o {server.OBFS}");
                if (!string.IsNullOrEmpty(server.OBFSParam)) argument.Append($" -g \"{server.OBFSParam}\"");
            }

            argument.Append($" -b {LocalAddress ?? Global.Settings.LocalAddress} -l {Socks5LocalPort ?? Global.Settings.Socks5LocalPort} -u");
            if (mode.BypassChina) argument.Append(" --acl default.acl");

            #endregion

            return StartInstanceAuto(argument.ToString());
        }

        public override void Stop()
        {
            StopInstance();
        }
    }
}