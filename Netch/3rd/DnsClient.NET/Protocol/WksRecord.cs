using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace DnsClient.Protocol
{
    /*
    https://tools.ietf.org/html/rfc1035#section-3.4.2:
    3.4.2. WKS RDATA format

        +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
        |                    ADDRESS                    |
        +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
        |       PROTOCOL        |                       |
        +--+--+--+--+--+--+--+--+                       |
        |                                               |
        /                   <BIT MAP>                   /
        /                                               /
        +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+

    where:

    ADDRESS         An 32 bit Internet address

    PROTOCOL        An 8 bit IP protocol number

    <BIT MAP>       A variable length bit map.  The bit map must be a
                    multiple of 8 bits long.

    The WKS record is used to describe the well known services supported by
    a particular protocol on a particular internet address.  The PROTOCOL
    field specifies an IP protocol number, and the bit map has one bit per
    port of the specified protocol.  The first bit corresponds to port 0,
    the second to port 1, etc.  If the bit map does not include a bit for a
    protocol of interest, that bit is assumed zero.  The appropriate values
    and mnemonics for ports and protocols are specified in [RFC-1010].

    For example, if PROTOCOL=TCP (6), the 26th bit corresponds to TCP port
    25 (SMTP).  If this bit is set, a SMTP server should be listening on TCP
    port 25; if zero, SMTP service is not supported on the specified
    address.

    The purpose of WKS RRs is to provide availability information for
    servers for TCP and UDP.  If a server supports both TCP and UDP, or has
    multiple Internet addresses, then multiple WKS RRs are used.

    WKS RRs cause no additional section processing.

    In master files, both ports and protocols are expressed using mnemonics
    or decimal numbers.

    ** remark:
    * RFS-1010 is obsolete/history.
    * The most current one is https://tools.ietf.org/html/rfc3232
    * The lists of protocols and ports are now handled via the online database on http://www.iana.org/.
    *
    * Also, see https://tools.ietf.org/html/rfc6335
    * For clarification which protocols are supported etc.
    */

    /// <summary>
    /// A <see cref="DnsResourceRecord"/> represending a Well Known Service description.
    /// </summary>
    /// <remarks>
    /// Instead of describing the supported protocols in RFCs, the list is now published on http://www.iana.org/.
    /// </remarks>
    /// <seealso href="http://www.iana.org/assignments/protocol-numbers/protocol-numbers.xhtml"/>
    /// <seealso href="https://tools.ietf.org/html/rfc3232">RFC 3232, the most current update.</seealso>
    public class WksRecord : DnsResourceRecord
    {
        /// <summary>
        /// Gets the address.
        /// </summary>
        /// <value>
        /// The address.
        /// </value>
        public IPAddress Address { get; }

        /// <summary>
        /// Gets the Protocol.
        /// </summary>
        /// <remarks>
        /// According to https://tools.ietf.org/html/rfc6335, only ports for TCP, UDP, DCCP and SCTP services will be assigned.
        /// </remarks>
        public ProtocolType Protocol { get; }

        /// <summary>
        /// Gets the binary raw bitmap.
        /// Use <see cref="Ports"/> to determine which ports are actually configured.
        /// </summary>
        public byte[] Bitmap { get; }

        /// <summary>
        /// Gets the list of assigned ports.
        /// <para>
        /// For example, if this list contains port 25, which is assigned to
        /// the <c>SMTP</c> service. This means that a SMTP services
        /// is running on <see cref="Address"/> with transport <see cref="Protocol"/>.
        /// </para>
        /// </summary>
        /// <seealso href="http://www.iana.org/assignments/port-numbers">Port numbers</seealso>
        public int[] Ports { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="WksRecord" /> class.
        /// </summary>
        /// <param name="info">The information.</param>
        /// <param name="address">The address.</param>
        /// <param name="protocol">The protocol.</param>
        /// <param name="bitmap">The raw data.</param>
        /// <exception cref="System.ArgumentNullException">
        /// If <paramref name="address"/> or <paramref name="info"/> or <paramref name="bitmap"/> is null.
        /// </exception>
        public WksRecord(ResourceRecordInfo info, IPAddress address, int protocol, byte[] bitmap)
            : base(info)
        {
            Address = address ?? throw new ArgumentNullException(nameof(address));
            Protocol = (ProtocolType)protocol;
            Bitmap = bitmap ?? throw new ArgumentNullException(nameof(bitmap));
            Ports = GetPorts(bitmap);
        }

        private protected override string RecordToString()
        {
            return $"{Address} {Protocol} {string.Join(" ", Ports)}";
        }

        private static int[] GetPorts(byte[] data)
        {
            int pos = 0, len = data.Length;

            var result = new List<int>();
            if (data.Length == 0)
            {
                return result.ToArray();
            }

            while (pos < len)
            {
                byte b = data[pos++];

                if (b != 0)
                {
                    for (int bit = 7; bit >= 0; bit--)
                    {
                        if ((b & (1 << bit)) != 0)
                        {
                            result.Add(pos * 8 - bit - 1);
                        }
                    }
                }
            }

            return result.ToArray();
        }
    }
}