using DnsClient.Protocol.Options;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace DnsClient
{
    internal abstract class DnsMessageHandler
    {
        public abstract DnsResponseMessage Query(IPEndPoint endpoint, DnsRequestMessage request, TimeSpan timeout);

        public abstract Task<DnsResponseMessage> QueryAsync(IPEndPoint endpoint, DnsRequestMessage request, CancellationToken cancellationToken,
            Action<Action> cancelationCallback);

        public abstract bool IsTransientException<T>(T exception) where T : Exception;

        public virtual void GetRequestData(DnsRequestMessage request, DnsDatagramWriter writer)
        {
            var question = request.Question;

            /*
                                                1  1  1  1  1  1
                  0  1  2  3  4  5  6  7  8  9  0  1  2  3  4  5
                +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
                |                      ID                       |
                +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
                |QR|   Opcode  |AA|TC|RD|RA|   Z    |   RCODE   |
                +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
                |                    QDCOUNT                    |
                +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
                |                    ANCOUNT                    |
                +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
                |                    NSCOUNT                    |
                +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
                |                    ARCOUNT                    |
                +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
            * */
            // 4 more bytes for the type and class

            writer.WriteInt16NetworkOrder((short)request.Header.Id);
            writer.WriteUInt16NetworkOrder(request.Header.RawFlags);
            writer.WriteInt16NetworkOrder(1);   // we support single question only... (as most DNS servers anyways).
            writer.WriteInt16NetworkOrder(0);
            writer.WriteInt16NetworkOrder(0);
            writer.WriteInt16NetworkOrder(1); // one additional for the Opt record.

            writer.WriteHostName(question.QueryName);
            writer.WriteUInt16NetworkOrder((ushort)question.QuestionType);
            writer.WriteUInt16NetworkOrder((ushort)question.QuestionClass);

            /*
               +------------+--------------+------------------------------+
               | Field Name | Field Type   | Description                  |
               +------------+--------------+------------------------------+
               | NAME       | domain name  | MUST be 0 (root domain)      |
               | TYPE       | u_int16_t    | OPT (41)                     |
               | CLASS      | u_int16_t    | requestor's UDP payload size |
               | TTL        | u_int32_t    | extended RCODE and flags     |
               | RDLEN      | u_int16_t    | length of all RDATA          |
               | RDATA      | octet stream | {attribute,value} pairs      |
               +------------+--------------+------------------------------+
            * */

            var opt = new OptRecord();

            writer.WriteHostName("");
            writer.WriteUInt16NetworkOrder((ushort)opt.RecordType);
            writer.WriteUInt16NetworkOrder((ushort)opt.RecordClass);
            writer.WriteUInt32NetworkOrder((ushort)opt.InitialTimeToLive);
            writer.WriteUInt16NetworkOrder(0);
        }

        public virtual DnsResponseMessage GetResponseMessage(ArraySegment<byte> responseData)
        {
            var reader = new DnsDatagramReader(responseData);
            var factory = new DnsRecordFactory(reader);

            var id = reader.ReadUInt16NetworkOrder();
            var flags = reader.ReadUInt16NetworkOrder();
            var questionCount = reader.ReadUInt16NetworkOrder();
            var answerCount = reader.ReadUInt16NetworkOrder();
            var nameServerCount = reader.ReadUInt16NetworkOrder();
            var additionalCount = reader.ReadUInt16NetworkOrder();

            var header = new DnsResponseHeader(id, flags, questionCount, answerCount, additionalCount, nameServerCount);
            var response = new DnsResponseMessage(header, responseData.Count);

            for (int questionIndex = 0; questionIndex < questionCount; questionIndex++)
            {
                var question = new DnsQuestion(reader.ReadQuestionQueryString(), (QueryType)reader.ReadUInt16NetworkOrder(), (QueryClass)reader.ReadUInt16NetworkOrder());
                response.AddQuestion(question);
            }

            for (int answerIndex = 0; answerIndex < answerCount; answerIndex++)
            {
                var info = factory.ReadRecordInfo();
                var record = factory.GetRecord(info);
                response.AddAnswer(record);
            }

            for (int serverIndex = 0; serverIndex < nameServerCount; serverIndex++)
            {
                var info = factory.ReadRecordInfo();
                var record = factory.GetRecord(info);
                response.AddAuthority(record);
            }

            for (int additionalIndex = 0; additionalIndex < additionalCount; additionalIndex++)
            {
                var info = factory.ReadRecordInfo();
                var record = factory.GetRecord(info);
                response.AddAdditional(record);
            }

            return response;
        }
    }
}