using DnsClient.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DnsClient
{
    internal class DnsResponseMessage
    {
        public DnsResponseMessage(DnsResponseHeader header, int messageSize)
        {
            Header = header ?? throw new ArgumentNullException(nameof(header));
            MessageSize = messageSize;
        }

        public List<DnsResourceRecord> Additionals { get; } = new List<DnsResourceRecord>();

        public List<DnsResourceRecord> Answers { get; } = new List<DnsResourceRecord>();

        public List<DnsResourceRecord> Authorities { get; } = new List<DnsResourceRecord>();

        public DnsResponseHeader Header { get; }

        public int MessageSize { get; }

        public List<DnsQuestion> Questions { get; } = new List<DnsQuestion>();

        public void AddAdditional(DnsResourceRecord record)
        {
            if (record == null)
            {
                throw new ArgumentNullException(nameof(record));
            }

            Additionals.Add(record);
        }

        public void AddAnswer(DnsResourceRecord record)
        {
            if (record == null)
            {
                throw new ArgumentNullException(nameof(record));
            }

            Answers.Add(record);
        }

        public void AddAuthority(DnsResourceRecord record)
        {
            if (record == null)
            {
                throw new ArgumentNullException(nameof(record));
            }

            Authorities.Add(record);
        }

        public void AddQuestion(DnsQuestion question)
        {
            if (question == null)
            {
                throw new ArgumentNullException(nameof(question));
            }

            Questions.Add(question);
        }

        public LookupClientAudit Audit { get; set; }

        /// <summary>
        /// Gets the readonly representation of this message which can be returned.
        /// </summary>
        public DnsQueryResponse AsQueryResponse(NameServer nameServer, DnsQuerySettings settings)
            => new DnsQueryResponse(this, nameServer, Audit, settings);

        public static DnsResponseMessage Combine(ICollection<DnsResponseMessage> messages)
        {
            if (messages.Count <= 1)
            {
                return messages.FirstOrDefault();
            }

            var first = messages.First();

            var header = new DnsResponseHeader(
                first.Header.Id,
                (ushort)first.Header.HeaderFlags,
                first.Header.QuestionCount,
                messages.Sum(p => p.Header.AnswerCount),
                messages.Sum(p => p.Header.AdditionalCount),
                first.Header.NameServerCount);

            var response = new DnsResponseMessage(header, messages.Sum(p => p.MessageSize));

            response.Questions.AddRange(first.Questions);
            response.Additionals.AddRange(messages.SelectMany(p => p.Additionals));
            response.Answers.AddRange(messages.SelectMany(p => p.Answers));
            response.Authorities.AddRange(messages.SelectMany(p => p.Authorities));

            return response;
        }
    }
}