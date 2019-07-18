using System;

namespace DnsClient.Protocol
{
    /*
    https://tools.ietf.org/html/rfc1035#section-3.3.8:
    3.3.8. MR RDATA format (EXPERIMENTAL)

        +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
        /                   NEWNAME                     /
        /                                               /
        +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+

    where:

    NEWNAME         A <domain-name> which specifies a mailbox which is the
                    proper rename of the specified mailbox.

    MR records cause no additional section processing.  The main use for MR
    is as a forwarding entry for a user who has moved to a different
    mailbox.
     */

    /// <summary>
    /// A <see cref="DnsResourceRecord"/> represending a mailbox rename domain name.
    /// </summary>
    /// <seealso href="https://tools.ietf.org/html/rfc1035#section-3.3.8">RFC 1035</seealso>
    public class MrRecord : DnsResourceRecord
    {
        /// <summary>
        /// Gets the domain name which specifies a mailbox which is the proper rename of the specified mailbox.
        /// </summary>
        /// <value>
        /// The domain name.
        /// </value>
        public DnsString NewName { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MrRecord"/> class.
        /// </summary>
        /// <param name="info">The information.</param>
        /// <param name="name">The domain name.</param>
        /// <exception cref="System.ArgumentNullException">If <paramref name="name"/> or <paramref name="info"/> is null.</exception>
        public MrRecord(ResourceRecordInfo info, DnsString name)
            : base(info)
        {
            NewName = name ?? throw new ArgumentNullException(nameof(name));
        }

        private protected override string RecordToString()
        {
            return NewName.Value;
        }
    }
}