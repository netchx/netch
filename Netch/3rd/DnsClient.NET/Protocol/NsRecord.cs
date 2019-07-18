using System;

namespace DnsClient.Protocol
{
    /*
    https://tools.ietf.org/html/rfc1035#section-3.3.11:
    NS RDATA format

        +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
        /                   NSDNAME                     /
        /                                               /
        +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+

    where:

    NSDNAME         A <domain-name> which specifies a host which should be
                authoritative for the specified class and domain.

    NS records cause both the usual additional section processing to locate
    a type A record, and, when used in a referral, a special search of the
    zone in which they reside for glue information.

    The NS RR states that the named host should be expected to have a zone
    starting at owner name of the specified class.  Note that the class may
    not indicate the protocol family which should be used to communicate
    with the host, although it is typically a strong hint.  For example,
    hosts which are name servers for either Internet (IN) or Hesiod (HS)
    class information are normally queried using IN class protocols.
     */

    /// <summary>
    /// A <see cref="DnsResourceRecord"/> represending an authoritative name server.
    /// </summary>
    /// <seealso href="https://tools.ietf.org/html/rfc1035#section-3.3.11">RFC 1035</seealso>
    public class NsRecord : DnsResourceRecord
    {
        /// <summary>
        /// Gets the domain name which specifies a host which should be authoritative for the specified class and domain.
        /// </summary>
        /// <value>
        /// The domain name.
        /// </value>
        public DnsString NSDName { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="NsRecord"/> class.
        /// </summary>
        /// <param name="info">The information.</param>
        /// <param name="name">The name.</param>
        /// <exception cref="System.ArgumentNullException">If <paramref name="name"/> or <paramref name="info"/> is null.</exception>
        public NsRecord(ResourceRecordInfo info, DnsString name)
            : base(info)
        {
            NSDName = name ?? throw new ArgumentNullException(nameof(name));
        }

        private protected override string RecordToString()
        {
            return NSDName.Value;
        }
    }
}