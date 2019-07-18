using System;

namespace DnsClient.Protocol
{
    /* https://tools.ietf.org/html/rfc1035#section-3.3.9
    3.3.9. MX RDATA format

        +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
        |                  PREFERENCE                   |
        +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
        /                   EXCHANGE                    /
        /                                               /
        +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+

    where:

    PREFERENCE      A 16 bit integer which specifies the preference given to
                    this RR among others at the same owner.  Lower values
                    are preferred.

    EXCHANGE        A <domain-name> which specifies a host willing to act as
                    a mail exchange for the owner name.

    MX records cause type A additional section processing for the host
    specified by EXCHANGE.  The use of MX RRs is explained in detail in
    [RFC-974].
    */

    /// <summary>
    /// A <see cref="DnsResourceRecord"/> represending a mail exchange.
    /// </summary>
    /// <seealso href="https://tools.ietf.org/html/rfc1035#section-3.3.9">RFC 1035</seealso>
    /// <seealso href="https://tools.ietf.org/html/rfc974">RFC 974</seealso>
    public class MxRecord : DnsResourceRecord
    {
        /// <summary>
        /// Gets a 16 bit integer which specifies the preference given to
        /// this RR among others at the same owner.
        /// Lower values are preferred.
        /// </summary>
        public ushort Preference { get; }

        /// <summary>
        /// A domain name which specifies a host willing to act as a mail exchange.
        /// </summary>
        public DnsString Exchange { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MxRecord"/> class.
        /// </summary>
        /// <param name="info">The information.</param>
        /// <param name="preference">The preference.</param>
        /// <param name="domainName">Name of the domain.</param>
        /// <exception cref="System.ArgumentNullException">If <paramref name="domainName"/> or <paramref name="info"/> is null.</exception>
        public MxRecord(ResourceRecordInfo info, ushort preference, DnsString domainName)
            : base(info)
        {
            Preference = preference;
            Exchange = domainName ?? throw new ArgumentNullException(nameof(domainName));
        }

        private protected override string RecordToString()
        {
            return string.Format("{0} {1}", Preference, Exchange);
        }
    }
}