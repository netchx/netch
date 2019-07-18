using System.Net;

namespace DnsClient.Protocol
{
    /// <summary>
    /// A <see cref="DnsResourceRecord"/> represending an IPv6 <see cref="IPAddress"/>.
    /// <para>
    /// A 128 bit IPv6 address is encoded in the data portion of an AAAA
    /// resource record in network byte order(high-order byte first).
    /// </para>
    /// </summary>
    /// <seealso href="https://tools.ietf.org/html/rfc3596#section-2.2">RFC 3596</seealso>
    public class AaaaRecord : AddressRecord
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AaaaRecord"/> class.
        /// </summary>
        /// <inheritdoc />
        public AaaaRecord(ResourceRecordInfo info, IPAddress address) : base(info, address)
        {
        }
    }
}