using DnsClient.Protocol;
using System.Collections.Generic;

namespace DnsClient
{
    /// <summary>
    /// Contract defining the result of a query performed by <see cref="IDnsQuery"/>.
    /// </summary>
    /// <seealso cref="IDnsQuery"/>
    /// <seealso cref="ILookupClient"/>
    public interface IDnsQueryResponse
    {
        /// <summary>
        /// Gets the list of questions.
        /// </summary>
        IReadOnlyList<DnsQuestion> Questions { get; }

        /// <summary>
        /// Gets a list of additional records.
        /// </summary>
        IReadOnlyList<DnsResourceRecord> Additionals { get; }

        /// <summary>
        /// Gets a list of all answers, addtional and authority records.
        /// </summary>
        IEnumerable<DnsResourceRecord> AllRecords { get; }

        /// <summary>
        /// Gets a list of answer records.
        /// </summary>
        IReadOnlyList<DnsResourceRecord> Answers { get; }

        /// <summary>
        /// Gets a list of authority records.
        /// </summary>
        IReadOnlyList<DnsResourceRecord> Authorities { get; }

        /// <summary>
        /// Gets the audit trail if <see cref="ILookupClient.EnableAuditTrail"/>. as set to <c>true</c>, <c>null</c> otherwise.
        /// </summary>
        /// <value>
        /// The audit trail.
        /// </value>
        string AuditTrail { get; }

        /// <summary>
        /// Returns a string value representing the error response code in case an error occured,
        /// otherwise '<see cref="DnsResponseCode.NoError"/>'.
        /// </summary>
        string ErrorMessage { get; }

        /// <summary>
        /// A flag indicating if the header contains a response codde other than <see cref="DnsResponseCode.NoError"/>.
        /// </summary>
        bool HasError { get; }

        /// <summary>
        /// Gets the header of the response.
        /// </summary>
        DnsResponseHeader Header { get; }

        /// <summary>
        /// Gets the size of the message.
        /// </summary>
        /// <value>
        /// The size of the message.
        /// </value>
        int MessageSize { get; }

        /// <summary>
        /// Gets the name server which responded with this result.
        /// </summary>
        /// <value>
        /// The name server.
        /// </value>
        NameServer NameServer { get; }

        /// <summary>
        /// Gets the settings used to produce this response.
        /// </summary>
        DnsQuerySettings Settings { get; }
    }
}