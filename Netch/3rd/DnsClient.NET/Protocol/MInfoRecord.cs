using System;
namespace DnsClient.Protocol
{
    /*
    https://tools.ietf.org/html/rfc1035#section-3.3.11:
    3.3.7. MINFO RDATA format (EXPERIMENTAL)

        +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
        /                    RMAILBX                    /
        +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
        /                    EMAILBX                    /
        +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+

    where:

    RMAILBX         A <domain-name> which specifies a mailbox which is
                    responsible for the mailing list or mailbox.  If this
                    domain name names the root, the owner of the MINFO RR is
                    responsible for itself.  Note that many existing mailing
                    lists use a mailbox X-request for the RMAILBX field of
                    mailing list X, e.g., Msgroup-request for Msgroup.  This
                    field provides a more general mechanism.

    EMAILBX         A <domain-name> which specifies a mailbox which is to
                    receive error messages related to the mailing list or
                    mailbox specified by the owner of the MINFO RR (similar
                    to the ERRORS-TO: field which has been proposed).  If
                    this domain name names the root, errors should be
                    returned to the sender of the message.

    MINFO records cause no additional section processing.  Although these
    records can be associated with a simple mailbox, they are usually used
    with a mailing list.
     */

    /// <summary>
    /// A <see cref="DnsResourceRecord"/> represending mailbox or mail list information.
    /// </summary>
    /// <seealso href="https://tools.ietf.org/html/rfc1035#section-3.3.11">RFC 1035</seealso>
    public class MInfoRecord : DnsResourceRecord
    {
        /// <summary>
        /// Gets the domain name which specifies a mailbox which is responsible for the mailing list or mailbox.
        /// </summary>
        /// <value>
        /// The domain name.
        /// </value>
        public DnsString RMailBox { get; }

        /// <summary>
        /// Gets the domain name which specifies a mailbox which is to receive error messages related to the mailing list or mailbox.
        /// </summary>
        /// <value>
        /// The domain name.
        /// </value>
        public DnsString EmailBox { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MInfoRecord"/> class.
        /// </summary>
        /// <param name="info">The information.</param>
        /// <param name="rmailBox">The <c>RMAILBX</c>.</param>
        /// <param name="emailBox">The <c>EMAILBX</c>.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="info"/> or <paramref name="rmailBox"/> or <paramref name="emailBox"/> is null.</exception>
        public MInfoRecord(ResourceRecordInfo info, DnsString rmailBox, DnsString emailBox)
            : base(info)
        {
            RMailBox = rmailBox ?? throw new ArgumentNullException(nameof(rmailBox));
            EmailBox = emailBox ?? throw new ArgumentNullException(nameof(emailBox));
        }

        private protected override string RecordToString()
        {
            return $"{RMailBox} {EmailBox}";
        }
    }
}