using System;

namespace DnsClient.Protocol
{
    /*
    https://tools.ietf.org/html/rfc1035#section-3.3.1:
    3.3.1. CNAME RDATA format

        +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
        /                     CNAME                     /
        /                                               /
        +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+

    where:

    CNAME           A <domain-name> which specifies the canonical or primary
                    name for the owner.  The owner name is an alias.

    CNAME RRs cause no additional section processing, but name servers may
    choose to restart the query at the canonical name in certain cases.  See
    the description of name server logic in [RFC-1034] for details.

     */

    /// <summary>
    /// A <see cref="DnsResourceRecord"/> repesenting the canonical name for an alias.
    /// </summary>
    /// <seealso href="https://tools.ietf.org/html/rfc1035#section-3.3.1">RFC 1035</seealso>
    public class CNameRecord : DnsResourceRecord
    {
        /// <summary>
        /// Gets the canonical name for an alias.
        /// </summary>
        /// <value>
        /// The canonical name.
        /// </value>
        public DnsString CanonicalName { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CNameRecord"/> class.
        /// </summary>
        /// <param name="info">The information.</param>
        /// <param name="canonicalName">The canonical name.</param>
        /// <exception cref="System.ArgumentNullException">If <paramref name="canonicalName"/> or <paramref name="info"/> is null.</exception>
        public CNameRecord(ResourceRecordInfo info, DnsString canonicalName)
            : base(info)
        {
            CanonicalName = canonicalName ?? throw new ArgumentNullException(nameof(canonicalName));
        }

        private protected override string RecordToString()
        {
            return CanonicalName.Value;
        }
    }
}