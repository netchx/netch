using System.Diagnostics;
using System.IO;
using Netch.Models;

namespace Netch.Controllers
{
    public class PrivoxyController : Controller
    {
        public PrivoxyController()
        {
            MainName = "Privoxy";
            ExtFiles = new[] {"default.conf"};
            ready = BeforeStartProgress();
        }

        public bool Start(Server server, Mode mode)
        {
            if (server.Type != "Socks5")
                File.WriteAllText("data\\privoxy.conf", File.ReadAllText("bin\\default.conf").Replace("_BIND_PORT_", Global.Settings.HTTPLocalPort.ToString()).Replace("_DEST_PORT_", Global.Settings.Socks5LocalPort.ToString()).Replace("0.0.0.0", Global.Settings.LocalAddress));
            else
                File.WriteAllText("data\\privoxy.conf", File.ReadAllText("bin\\default.conf").Replace("_BIND_PORT_", Global.Settings.HTTPLocalPort.ToString()).Replace("_DEST_PORT_", server.Port.ToString()).Replace("s 0.0.0.0", $"s {Global.Settings.LocalAddress}").Replace("/ 127.0.0.1", $"/ {server.Hostname}"));


            Instance = new Process
            {
                StartInfo =
                {
                    FileName = $"{Global.NetchDir}\\bin\\Privoxy.exe",
                    Arguments = "..\\data\\privoxy.conf",
                    WorkingDirectory = $"{Global.NetchDir}\\bin",
                    WindowStyle = ProcessWindowStyle.Hidden,
                    UseShellExecute = true,
                    CreateNoWindow = true
                }
            };
            Instance.Start();

            return true;
        }
    }
}