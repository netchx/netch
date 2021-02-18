using System.IO;
using System.Text;
using Netch.Models;
using Netch.Servers.Socks5;

namespace Netch.Controllers
{
    public class PrivoxyController : Guard, IController
    {
        public PrivoxyController()
        {
            RedirectStd = false;
        }

        public override string MainFile { get; protected set; } = "Privoxy.exe";

        public override string Name { get; } = "Privoxy";

        public override void Stop()
        {
            StopInstance();
        }

        public void Start(Server server)
        {
            var text = new StringBuilder(File.ReadAllText("bin\\default.conf"));

            text.Replace("_BIND_PORT_", Global.Settings.HTTPLocalPort.ToString());
            text.Replace("0.0.0.0", Global.Settings.LocalAddress); /* BIND_HOST */

            if (server is Socks5 socks5 && !socks5.Auth())
            {
                text.Replace("/ 127.0.0.1", $"/ {server.AutoResolveHostname()}"); /* DEST_HOST */
                text.Replace("_DEST_PORT_", socks5.Port.ToString());
            }

            text.Replace("_DEST_PORT_", Global.Settings.Socks5LocalPort.ToString());


            File.WriteAllText("data\\privoxy.conf", text.ToString());

            StartInstanceAuto("..\\data\\privoxy.conf");
        }
    }
}