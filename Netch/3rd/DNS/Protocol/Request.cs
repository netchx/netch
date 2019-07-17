using DNS.Protocol.ResourceRecords;
using DNS.Protocol.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DNS.Protocol
{
    public class Request : IRequest
    {
        private static readonly Random RANDOM = new Random();

        private IList<Question> questions;
        private Header header;
        private IList<IResourceRecord> additional;

        public static Request FromArray(byte[] message)
        {
            Header header = Header.FromArray(message);
            int offset = header.Size;

            if (header.Response || header.QuestionCount == 0 ||
                    header.AnswerRecordCount + header.AuthorityRecordCount > 0 ||
                    header.ResponseCode != ResponseCode.NoError)
            {

                throw new ArgumentException("Invalid request message");
            }

            return new Request(header,
                Question.GetAllFromArray(message, offset, header.QuestionCount, out offset),
                ResourceRecordFactory.GetAllFromArray(message, offset, header.AdditionalRecordCount, out offset));
        }

        public Request(Header header, IList<Question> questions, IList<IResourceRecord> additional)
        {
            this.header = header;
            this.questions = questions;
            this.additional = additional;
        }

        public Request()
        {
            this.questions = new List<Question>();
            this.header = new Header();
            this.additional = new List<IResourceRecord>();

            this.header.OperationCode = OperationCode.Query;
            this.header.Response = false;
            this.header.Id = RANDOM.Next(UInt16.MaxValue);
        }

        public Request(IRequest request)
        {
            this.header = new Header();
            this.questions = new List<Question>(request.Questions);
            this.additional = new List<IResourceRecord>(request.AdditionalRecords);

            this.header.Response = false;

            Id = request.Id;
            OperationCode = request.OperationCode;
            RecursionDesired = request.RecursionDesired;
        }

        public IList<Question> Questions
        {
            get { return questions; }
        }

        public IList<IResourceRecord> AdditionalRecords
        {
            get { return additional; }
        }

        public int Size
        {
            get
            {
                return header.Size +
                    questions.Sum(q => q.Size) +
                    additional.Sum(a => a.Size);
            }
        }

        public int Id
        {
            get { return header.Id; }
            set { header.Id = value; }
        }

        public OperationCode OperationCode
        {
            get { return header.OperationCode; }
            set { header.OperationCode = value; }
        }

        public bool RecursionDesired
        {
            get { return header.RecursionDesired; }
            set { header.RecursionDesired = value; }
        }

        public byte[] ToArray()
        {
            UpdateHeader();
            ByteStream result = new ByteStream(Size);

            result
                .Append(header.ToArray())
                .Append(questions.Select(q => q.ToArray()))
                .Append(additional.Select(a => a.ToArray()));

            return result.ToArray();
        }

        public override string ToString()
        {
            UpdateHeader();

            return ObjectStringifier.New(this)
                .Add("Header", header)
                .Add("Questions", "AdditionalRecords")
                .ToString();
        }

        private void UpdateHeader()
        {
            header.QuestionCount = questions.Count;
            header.AdditionalRecordCount = additional.Count;
        }
    }
}
