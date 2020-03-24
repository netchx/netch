using System;
using System.Net;

namespace Netch.Override
{
    public class WebClient : System.Net.WebClient
    {
        protected override WebRequest GetWebRequest(Uri address)
        {
            var request = base.GetWebRequest(address);
            request.Timeout = 10000;
            ((HttpWebRequest)request).ReadWriteTimeout = 10000;

            return request;
        }
    }
}
