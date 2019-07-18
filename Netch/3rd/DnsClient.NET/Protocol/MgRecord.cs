using System;

namespace DnsClient.Protocol
{
    /*
    https://tools.ietf.org/html/rfc1035#section-3.3.6:
    3.3.6. MG RDATA format (EXPERIMENTAL)

        +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
        /                   MGMNAME                     /
        /                                               /
        +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+

    where:

    MGMNAME         A <domain-name> which specifies a mailbox which is a
                    member of the mail group specified by the domain name.

    MG records cause no additional section processing.

     */

    /// <summary>
    /// A <see cref="DnsResourceRecord"/> represending a domain name which specifies a mailbox which is a member of the mail group specified by the domain name.
    /// </summary>
    /// <seealso href="https://tools.ietf.org/html/rfc1035#section-3.3.6">RFC 1035</seealso>
    public class MgRecord : DnsResourceRecord
    {
        /// <summary>
        /// Gets a domain name which specifies a mailbox which is a member of the mail group specified by the domain nam.
        /// </summary>
        /// <value>
        /// The domain name.
        /// </value>
        public DnsString MgName { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MgRecord"/> class.
        /// </summary>
        /// <param name="info">The information.</param>
        /// <param name="domainName">The domain name.</param>
        /// <exception cref="System.ArgumentNullException">If <paramref name="domainName"/> or <paramref name="info"/> is null.</exception>
        public MgRecord(ResourceRecordInfo info, DnsString domainName)
            : base(info)
        {
            MgName = domainName ?? throw new ArgumentNullException(nameof(domainName));
        }

        private protected override string RecordToString()
        {
            return MgName.Value;
        }
    }
}