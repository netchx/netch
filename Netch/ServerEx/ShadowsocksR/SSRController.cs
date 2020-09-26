using System.Text;
using Netch.Models;

namespace Netch.ServerEx.ShadowsocksR
{
    public class SSRController : ServerController
    {
        public SSRController()
        {
            Name = "ShadowsocksR";
            MainFile = "ShadowsocksR.exe";
        }


        public override bool Start(Server s, Mode mode)
        {
            var server = (ShadowsocksR) s;

            #region Argument

            var argument = new StringBuilder();
            argument.Append($"-s {server.Hostname} -p {server.Port} -k \"{server.Password}\" -m {server.EncryptMethod} -t 120");
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

            argument.Append($" -b {LocalAddress} -l {Socks5LocalPort} -u");
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