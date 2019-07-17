using DNS.Client.RequestResolver;
using DNS.Protocol;
using DNS.Protocol.ResourceRecords;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace DNS.Client
{
    public class ClientRequest : IRequest
    {
        private const int DEFAULT_PORT = 53;

        private IRequestResolver resolver;
        private IRequest request;

        public ClientRequest(IPEndPoint dns, IRequest request = null) :
            this(new UdpRequestResolver(dns), request)
        { }

        public ClientRequest(IPAddress ip, int port = DEFAULT_PORT, IRequest request = null) :
            this(new IPEndPoint(ip, port), request)
        { }

        public ClientRequest(string ip, int port = DEFAULT_PORT, IRequest request = null) :
            this(IPAddress.Parse(ip), port, request)
        { }

        public ClientRequest(IRequestResolver resolver, IRequest request = null)
        {
            this.resolver = resolver;
            this.request = request == null ? new Request() : new Request(request);
        }

        public int Id
        {
            get { return request.Id; }
            set { request.Id = value; }
        }

        public IList<IResourceRecord> AdditionalRecords
        {
            get { return new ReadOnlyCollection<IResourceRecord>(request.AdditionalRecords); }
        }

        public OperationCode OperationCode
        {
            get { return request.OperationCode; }
            set { request.OperationCode = value; }
        }

        public bool RecursionDesired
        {
            get { return request.RecursionDesired; }
            set { request.RecursionDesired = value; }
        }

        public IList<Question> Questions
        {
            get { return request.Questions; }
        }

        public int Size
        {
            get { return request.Size; }
        }

        public byte[] ToArray()
        {
            return request.ToArray();
        }

        public override string ToString()
        {
            return request.ToString();
        }

        /// <summary>
        /// Resolves this request into a response using the provided DNS information. The given
        /// request strategy is used to retrieve the response.
        /// </summary>
        /// <exception cref="ResponseException">Throw if a malformed response is received from the server</exception>
        /// <exception cref="IOException">Thrown if a IO error occurs</exception>
        /// <exception cref="SocketException">Thrown if the reading or writing to the socket fails</exception>
        /// <exception cref="OperationCanceledException">Thrown if reading or writing to the socket timeouts</exception>
        /// <returns>The response received from server</returns>
        public async Task<IResponse> Resolve(CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                IResponse response = await resolver.Resolve(this, cancellationToken);

                if (response.Id != this.Id)
                {
                    throw new ResponseException(response, "Mismatching request/response IDs");
                }
                if (response.ResponseCode != ResponseCode.NoError)
                {
                    throw new ResponseException(response);
                }

                return response;
            }
            catch (ArgumentException e)
            {
                throw new ResponseException("Invalid response", e);
            }
            catch (IndexOutOfRangeException e)
            {
                throw new ResponseException("Invalid response", e);
            }
        }
    }
}
