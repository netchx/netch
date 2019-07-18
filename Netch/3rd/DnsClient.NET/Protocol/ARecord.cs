using System.Net;

namespace DnsClient.Protocol
{
    /*
    3.4.1. A RDATA format

    +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
    |                    ADDRESS                    |
    +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+

    where:

    ADDRESS         A 32 bit Internet address.

    Hosts that have multiple Internet addresses will have multiple A
    records.
    *
    */

    /// <summary>
    /// A <see cref="DnsResourceRecord"/> represending an IPv4 <see cref="IPAddress"/>.
    /// Hosts that have multiple Internet addresses will have multiple A records.
    /// </summary>
    /// <seealso href="https://tools.ietf.org/html/rfc1035">RFC 1035</seealso>
    public class ARecord : AddressRecord
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ARecord"/> class.
        /// </summary>
        /// <inheritdoc />
        public ARecord(ResourceRecordInfo info, IPAddress address) : base(info, address)
        {
        }
    }
}