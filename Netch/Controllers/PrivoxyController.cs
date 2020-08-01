using System.IO;
using Netch.Models;

namespace Netch.Controllers
{
    public class PrivoxyController : Controller
    {
        public PrivoxyController()
        {
            Name = "Privoxy";
            MainFile = "Privoxy.exe";
            RedirectStd = false;
        }

        public bool Start(Server server, Mode mode)
        {
            var isSocks5 = server.Type == "Socks5";
            var socks5Port = isSocks5 ? server.Port : Global.Settings.Socks5LocalPort;
            var text = File.ReadAllText("bin\\default.conf")
                .Replace("_BIND_PORT_", Global.Settings.HTTPLocalPort.ToString())
                .Replace("_DEST_PORT_", socks5Port.ToString())
                .Replace("0.0.0.0", Global.Settings.LocalAddress);
            if (isSocks5)
                text = text.Replace("/ 127.0.0.1", $"/ {server.Hostname}");
            File.WriteAllText("data\\privoxy.conf", text);

            return StartInstanceAuto("..\\data\\privoxy.conf");
        }

        public override void Stop()
        {
            StopInstance();
        }
    }
}