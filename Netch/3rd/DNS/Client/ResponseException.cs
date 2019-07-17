using DNS.Protocol;
using System;

namespace DNS.Client
{
    public class ResponseException : Exception
    {
        private static string Format(IResponse response)
        {
            return string.Format("Invalid response received with code {0}", response.ResponseCode);
        }

        public ResponseException() { }
        public ResponseException(string message) : base(message) { }
        public ResponseException(string message, Exception e) : base(message, e) { }

        public ResponseException(IResponse response) : this(response, Format(response)) { }

        public ResponseException(IResponse response, Exception e)
            : base(Format(response), e)
        {
            Response = response;
        }

        public ResponseException(IResponse response, string message)
            : base(message)
        {
            Response = response;
        }

        public IResponse Response
        {
            get;
            private set;
        }
    }
}
