using DNS.Protocol;
using DNS.Protocol.Utils;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace DNS.Client.RequestResolver
{
    public class UdpRequestResolver : IRequestResolver
    {
        private int timeout;
        private IRequestResolver fallback;
        private IPEndPoint dns;

        public UdpRequestResolver(IPEndPoint dns, IRequestResolver fallback, int timeout = 5000)
        {
            this.dns = dns;
            this.fallback = fallback;
            this.timeout = timeout;
        }

        public UdpRequestResolver(IPEndPoint dns, int timeout = 5000)
        {
            this.dns = dns;
            this.fallback = new NullRequestResolver();
            this.timeout = timeout;
        }

        public async Task<IResponse> Resolve(IRequest request, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (UdpClient udp = new UdpClient())
            {
                await udp
                    .SendAsync(request.ToArray(), request.Size, dns)
                    .WithCancellationTimeout(TimeSpan.FromMilliseconds(timeout), cancellationToken);

                UdpReceiveResult result = await udp
                    .ReceiveAsync()
                    .WithCancellationTimeout(TimeSpan.FromMilliseconds(timeout), cancellationToken);

                if (!result.RemoteEndPoint.Equals(dns)) throw new IOException("Remote endpoint mismatch");
                byte[] buffer = result.Buffer;
                Response response = Response.FromArray(buffer);

                if (response.Truncated)
                {
                    return await fallback.Resolve(request, cancellationToken);
                }

                return new ClientResponse(request, response, buffer);
            }
        }
    }
}
