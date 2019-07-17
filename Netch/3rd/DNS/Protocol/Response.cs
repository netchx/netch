using DNS.Protocol.ResourceRecords;
using DNS.Protocol.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DNS.Protocol
{
    public class Response : IResponse
    {
        private static readonly Random RANDOM = new Random();

        private Header header;
        private IList<Question> questions;
        private IList<IResourceRecord> answers;
        private IList<IResourceRecord> authority;
        private IList<IResourceRecord> additional;

        public static Response FromRequest(IRequest request)
        {
            Response response = new Response();

            response.Id = request.Id;

            foreach (Question question in request.Questions)
            {
                response.Questions.Add(question);
            }

            return response;
        }

        public static Response FromArray(byte[] message)
        {
            Header header = Header.FromArray(message);
            int offset = header.Size;

            if (!header.Response || header.QuestionCount == 0)
            {
                throw new ArgumentException("Invalid response message");
            }

            if (header.Truncated)
            {
                return new Response(header,
                    Question.GetAllFromArray(message, offset, header.QuestionCount),
                    new List<IResourceRecord>(),
                    new List<IResourceRecord>(),
                    new List<IResourceRecord>());
            }

            return new Response(header,
                Question.GetAllFromArray(message, offset, header.QuestionCount, out offset),
                ResourceRecordFactory.GetAllFromArray(message, offset, header.AnswerRecordCount, out offset),
                ResourceRecordFactory.GetAllFromArray(message, offset, header.AuthorityRecordCount, out offset),
                ResourceRecordFactory.GetAllFromArray(message, offset, header.AdditionalRecordCount, out offset));
        }

        public Response(Header header, IList<Question> questions, IList<IResourceRecord> answers,
                IList<IResourceRecord> authority, IList<IResourceRecord> additional)
        {
            this.header = header;
            this.questions = questions;
            this.answers = answers;
            this.authority = authority;
            this.additional = additional;
        }

        public Response()
        {
            this.header = new Header();
            this.questions = new List<Question>();
            this.answers = new List<IResourceRecord>();
            this.authority = new List<IResourceRecord>();
            this.additional = new List<IResourceRecord>();

            this.header.Response = true;
            this.header.Id = RANDOM.Next(UInt16.MaxValue);
        }

        public Response(IResponse response)
        {
            this.header = new Header();
            this.questions = new List<Question>(response.Questions);
            this.answers = new List<IResourceRecord>(response.AnswerRecords);
            this.authority = new List<IResourceRecord>(response.AuthorityRecords);
            this.additional = new List<IResourceRecord>(response.AdditionalRecords);

            this.header.Response = true;

            Id = response.Id;
            RecursionAvailable = response.RecursionAvailable;
            AuthorativeServer = response.AuthorativeServer;
            OperationCode = response.OperationCode;
            ResponseCode = response.ResponseCode;
        }

        public IList<Question> Questions
        {
            get { return questions; }
        }

        public IList<IResourceRecord> AnswerRecords
        {
            get { return answers; }
        }

        public IList<IResourceRecord> AuthorityRecords
        {
            get { return authority; }
        }

        public IList<IResourceRecord> AdditionalRecords
        {
            get { return additional; }
        }

        public int Id
        {
            get { return header.Id; }
            set { header.Id = value; }
        }

        public bool RecursionAvailable
        {
            get { return header.RecursionAvailable; }
            set { header.RecursionAvailable = value; }
        }

        public bool AuthenticData
        {
            get { return header.AuthenticData; }
            set { header.AuthenticData = value; }
        }

        public bool CheckingDisabled
        {
            get { return header.CheckingDisabled; }
            set { header.CheckingDisabled = value; }
        }

        public bool AuthorativeServer
        {
            get { return header.AuthorativeServer; }
            set { header.AuthorativeServer = value; }
        }

        public bool Truncated
        {
            get { return header.Truncated; }
            set { header.Truncated = value; }
        }

        public OperationCode OperationCode
        {
            get { return header.OperationCode; }
            set { header.OperationCode = value; }
        }

        public ResponseCode ResponseCode
        {
            get { return header.ResponseCode; }
            set { header.ResponseCode = value; }
        }

        public int Size
        {
            get
            {
                return header.Size +
                    questions.Sum(q => q.Size) +
                    answers.Sum(a => a.Size) +
                    authority.Sum(a => a.Size) +
                    additional.Sum(a => a.Size);
            }
        }

        public byte[] ToArray()
        {
            UpdateHeader();
            ByteStream result = new ByteStream(Size);

            result
                .Append(header.ToArray())
                .Append(questions.Select(q => q.ToArray()))
                .Append(answers.Select(a => a.ToArray()))
                .Append(authority.Select(a => a.ToArray()))
                .Append(additional.Select(a => a.ToArray()));

            return result.ToArray();
        }

        public override string ToString()
        {
            UpdateHeader();

            return ObjectStringifier.New(this)
                .Add("Header", header)
                .Add("Questions", "AnswerRecords", "AuthorityRecords", "AdditionalRecords")
                .ToString();
        }

        private void UpdateHeader()
        {
            header.QuestionCount = questions.Count;
            header.AnswerRecordCount = answers.Count;
            header.AuthorityRecordCount = authority.Count;
            header.AdditionalRecordCount = additional.Count;
        }
    }
}
