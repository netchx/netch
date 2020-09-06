using System.Text;
using Netch.Models;

namespace Netch.Controllers
{
    public class SSRController : EncryptedProxy
    {
        public SSRController()
        {
            Name = "ShadowsocksR";
            MainFile = "ShadowsocksR.exe";
            StartedKeywords.Add("listening at");
            StoppedKeywords.AddRange(new[] {"Invalid config path", "usage"});
        }

        public override bool Start(Server server, Mode mode)
        {
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