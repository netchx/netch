using System;
using System.Net;
using System.Threading.Tasks;

namespace Netch.Controllers
{
    public class DNSController
    {
        public static DNS.Server.DnsServer Server = new DNS.Server.DnsServer(new Resolver());

        public bool Start()
        {
            try
            {
                Task.Run(() =>
                {
                    Server.Listen(new IPEndPoint(IPAddress.IPv6Any, 53)).Wait();
                });
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
