using DnsClient.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DnsClient
{
    /// <summary>
    /// The response returned by any query performed by <see cref="IDnsQuery"/> with all answer sections, header and message information.
    /// </summary>
    /// <seealso cref="IDnsQuery"/>
    /// <seealso cref="ILookupClient"/>
    public class DnsQueryResponse : IDnsQueryResponse
    {
        private int? _hashCode;

        /// <summary>
        /// Gets the name server which responded with this result.
        /// </summary>
        /// <value>
        /// The name server.
        /// </value>
        public NameServer NameServer { get; }

        /// <summary>
        /// Gets a list of additional records.
        /// </summary>
        public IReadOnlyList<DnsResourceRecord> Additionals { get; }

        /// <summary>
        /// Gets a list of all answers, addtional and authority records.
        /// </summary>
        public IEnumerable<DnsResourceRecord> AllRecords
        {
            get
            {
                return Answers.Concat(Additionals).Concat(Authorities);
            }
        }

        /// <summary>
        /// Gets the audit trail if <see cref="ILookupClient.EnableAuditTrail"/>. as set to <c>true</c>, <c>null</c> otherwise.
        /// </summary>
        /// <value>
        /// The audit trail.
        /// </value>
        public string AuditTrail => Audit?.Build(this);

        /// <summary>
        /// Gets a list of answer records.
        /// </summary>
        public IReadOnlyList<DnsResourceRecord> Answers { get; }

        /// <summary>
        /// Gets a list of authority records.
        /// </summary>
        public IReadOnlyList<DnsResourceRecord> Authorities { get; }

        /// <summary>
        /// Returns a string value representing the error response code in case an error occured,
        /// otherwise '<see cref="DnsResponseCode.NoError"/>'.
        /// </summary>
        public string ErrorMessage => DnsResponseCodeText.GetErrorText(Header.ResponseCode);

        /// <summary>
        /// A flag indicating if the header contains a response codde other than <see cref="DnsResponseCode.NoError"/>.
        /// </summary>
        public bool HasError => Header?.ResponseCode != DnsResponseCode.NoError;

        /// <summary>
        /// Gets the header of the response.
        /// </summary>
        public DnsResponseHeader Header { get; }

        /// <summary>
        /// Gets the list of questions.
        /// </summary>
        public IReadOnlyList<DnsQuestion> Questions { get; }

        /// <summary>
        /// Gets the size of the message.
        /// </summary>
        /// <value>
        /// The size of the message.
        /// </value>
        public int MessageSize { get; }

        /// <summary>
        /// Gets the settings used to produce this response.
        /// </summary>
        public DnsQuerySettings Settings { get; }

        internal LookupClientAudit Audit { get; }

        internal DnsQueryResponse(DnsResponseMessage dnsResponseMessage, NameServer nameServer, LookupClientAudit audit, DnsQuerySettings settings)
        {
            if (dnsResponseMessage == null) throw new ArgumentNullException(nameof(dnsResponseMessage));
            Header = dnsResponseMessage.Header;
            MessageSize = dnsResponseMessage.MessageSize;
            Questions = dnsResponseMessage.Questions.ToArray();
            Answers = dnsResponseMessage.Answers.ToArray();
            Additionals = dnsResponseMessage.Additionals.ToArray();
            Authorities = dnsResponseMessage.Authorities.ToArray();
            NameServer = nameServer ?? throw new ArgumentNullException(nameof(nameServer));
            Audit = audit;
            Settings = settings;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (!(obj is DnsQueryResponse response))
            {
                return false;
            }

            return
                Header.ToString().Equals(response.Header.ToString())
                && string.Join("", Questions).Equals(string.Join("", response.Questions))
                && string.Join("", AllRecords).Equals(string.Join("", response.AllRecords));
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            if (!_hashCode.HasValue)
            {
                _hashCode = (Header.ToString() + string.Join("", Questions) + string.Join("", AllRecords)).GetHashCode();
            }

            return _hashCode.Value;
        }
    }
}