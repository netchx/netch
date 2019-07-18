using System;

namespace DnsClient.Protocol
{
    /* RFC 1035 (https://tools.ietf.org/html/rfc1035#section-3.3.12)
    3.3.12. PTR RDATA format

        +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
        /                   PTRDNAME                    /
        +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+

    where:

    PTRDNAME        A <domain-name> which points to some location in the
                domain name space.

    PTR records cause no additional section processing.  These RRs are used
    in special domains to point to some other location in the domain space.
    These records are simple data, and don't imply any special processing
    similar to that performed by CNAME, which identifies aliases.  See the
    description of the IN-ADDR.ARPA domain for an example.
    */

    /// <summary>
    /// A <see cref="DnsResourceRecord"/> represending a pointer. These RRs are used
    /// in special domains to point to some other location in the domain space.
    /// </summary>
    /// <seealso cref="DnsClient.Protocol.DnsResourceRecord" />
    /// <seealso href="https://tools.ietf.org/html/rfc1035#section-3.3.12">RFC 1035</seealso>
    public class PtrRecord : DnsResourceRecord
    {
        /// <summary>
        /// Gets the domain name which points to some location in the domain name space.
        /// </summary>
        /// <value>
        /// The domain name.
        /// </value>
        public DnsString PtrDomainName { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PtrRecord"/> class.
        /// </summary>
        /// <param name="info">The information.</param>
        /// <param name="ptrDomainName">The domain name.</param>
        /// <exception cref="System.ArgumentNullException">If <paramref name="info"/> or <paramref name="ptrDomainName"/> is null.</exception>
        public PtrRecord(ResourceRecordInfo info, DnsString ptrDomainName)
            : base(info)
        {
            PtrDomainName = ptrDomainName ?? throw new ArgumentNullException(nameof(ptrDomainName));
        }

        private protected override string RecordToString()
        {
            return PtrDomainName.Value;
        }
    }
}