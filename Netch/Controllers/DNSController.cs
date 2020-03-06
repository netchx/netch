using Netch.Forms;
using System;
using System.Net;

namespace Netch.Controllers
{
    public class DNSController
    {
        public static DNS.Server.DnsServer Server = new DNS.Server.DnsServer(new Resolver());

        public bool Start()
        {
            MainForm.Instance.StatusText($"{Utils.i18N.Translate("Status")}{Utils.i18N.Translate(": ")}{Utils.i18N.Translate("Starting LocalDns service")}");
            try
            {
                _ = Server.Listen(new IPEndPoint(IPAddress.IPv6Any, 53));
            }
            catch (Exception e)
            {
                Utils.Logging.Info(e.ToString());
                return false;
            }

            return true;
        }

        public void Stop()
        {
            try
            {
                Server.Dispose();
            }
            catch (Exception e)
            {
                Utils.Logging.Info(e.ToString());
            }
        }
    }
}
