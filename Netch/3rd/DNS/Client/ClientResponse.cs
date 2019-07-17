using DNS.Protocol;
using DNS.Protocol.ResourceRecords;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DNS.Client
{
    public class ClientResponse : IResponse
    {
        private IResponse response;
        private byte[] message;

        public static ClientResponse FromArray(IRequest request, byte[] message)
        {
            Response response = Response.FromArray(message);
            return new ClientResponse(request, response, message);
        }

        internal ClientResponse(IRequest request, IResponse response, byte[] message)
        {
            Request = request;

            this.message = message;
            this.response = response;
        }

        internal ClientResponse(IRequest request, IResponse response)
        {
            Request = request;

            this.message = response.ToArray();
            this.response = response;
        }

        public IRequest Request
        {
            get;
            private set;
        }

        public int Id
        {
            get { return response.Id; }
            set { }
        }

        public IList<IResourceRecord> AnswerRecords
        {
            get { return response.AnswerRecords; }
        }

        public IList<IResourceRecord> AuthorityRecords
        {
            get { return new ReadOnlyCollection<IResourceRecord>(response.AuthorityRecords); }
        }

        public IList<IResourceRecord> AdditionalRecords
        {
            get { return new ReadOnlyCollection<IResourceRecord>(response.AdditionalRecords); }
        }

        public bool RecursionAvailable
        {
            get { return response.RecursionAvailable; }
            set { }
        }

        public bool AuthenticData
        {
            get { return response.AuthenticData; }
            set { }
        }

        public bool CheckingDisabled
        {
            get { return response.CheckingDisabled; }
            set { }
        }

        public bool AuthorativeServer
        {
            get { return response.AuthorativeServer; }
            set { }
        }

        public bool Truncated
        {
            get { return response.Truncated; }
            set { }
        }

        public OperationCode OperationCode
        {
            get { return response.OperationCode; }
            set { }
        }

        public ResponseCode ResponseCode
        {
            get { return response.ResponseCode; }
            set { }
        }

        public IList<Question> Questions
        {
            get { return new ReadOnlyCollection<Question>(response.Questions); }
        }

        public int Size
        {
            get { return message.Length; }
        }

        public byte[] ToArray()
        {
            return message;
        }

        public override string ToString()
        {
            return response.ToString();
        }
    }
}
