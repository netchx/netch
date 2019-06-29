using System;
using System.Net;

namespace Netch.Override
{
    public class WebClient : System.Net.WebClient
    {
        protected override WebRequest GetWebRequest(Uri address)
        {
            var request = base.GetWebRequest(address);
            request.Timeout = 4000;
            ((HttpWebRequest)request).ReadWriteTimeout = 4000;

            return request;
        }
    }
}
