using System;

namespace DnsClient.Protocol
{
    /*
    https://tools.ietf.org/html/rfc1183#section-2.2:
    2. Responsible Person

       The purpose of this section is to provide a standard method for
       associating responsible person identification to any name in the DNS.

       The domain name system functions as a distributed database which
       contains many different form of information.  For a particular name
       or host, you can discover it's Internet address, mail forwarding
       information, hardware type and operating system among others.

       A key aspect of the DNS is that the tree-structured namespace can be
       divided into pieces, called zones, for purposes of distributing
       control and responsibility.  The responsible person for zone database
       purposes is named in the SOA RR for that zone.  This section
       describes an extension which allows different responsible persons to
       be specified for different names in a zone.

    2.2. The Responsible Person RR

       The method uses a new RR type with mnemonic RP and type code of 17
       (decimal).

       RP has the following format:

       <owner> <ttl> <class> RP <mbox-dname> <txt-dname>

       Both RDATA fields are required in all RP RRs.

       The first field, <mbox-dname>, is a domain name that specifies the
       mailbox for the responsible person.  Its format in master files uses
       the DNS convention for mailbox encoding, identical to that used for
       the RNAME mailbox field in the SOA RR.  The root domain name (just
       ".") may be specified for <mbox-dname> to indicate that no mailbox is
       available.

       The second field, <txt-dname>, is a domain name for which TXT RR's
       exist.  A subsequent query can be performed to retrieve the
       associated TXT resource records at <txt-dname>.  This provides a
       level of indirection so that the entity can be referred to from
       multiple places in the DNS.  The root domain name (just ".") may be
       specified for <txt-dname> to indicate that the TXT_DNAME is absent,
       and no associated TXT RR exists.

       The format of the RP RR is class insensitive.  RP records cause no
       additional section processing.  (TXT additional section processing
       for <txt-dname> is allowed as an option, but only if it is disabled
       for the root, i.e., ".").

       The Responsible Person RR can be associated with any node in the
       Domain Name System hierarchy, not just at the leaves of the tree.

       The TXT RR associated with the TXT_DNAME contain free format text
       suitable for humans.  Refer to [4] for more details on the TXT RR.

       Multiple RP records at a single name may be present in the database.
       They should have identical TTLs.
    */

    /// <summary>
    /// A <see cref="DnsResourceRecord"/> represending a responsible person.
    /// </summary>
    /// <seealso href="https://tools.ietf.org/html/rfc1183#section-2.2">RFC 1183</seealso>
    public class RpRecord : DnsResourceRecord
    {
        /// <summary>
        /// Gets a domain name that specifies the mailbox for the responsible person.
        /// </summary>
        /// <value>
        /// The mailbox domain.
        /// </value>
        public DnsString MailboxDomainName { get; }

        /// <summary>
        /// Gets a domain name for which TXT RR's exist.
        /// </summary>
        /// <value>
        /// The text domain.
        /// </value>
        public DnsString TextDomainName { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RpRecord"/> class.
        /// </summary>
        /// <param name="info">The information.</param>
        /// <param name="mailbox">The mailbox domain.</param>
        /// <param name="textName">The text domain.</param>
        /// <exception cref="System.ArgumentNullException">
        /// If <paramref name="info"/> or <paramref name="mailbox"/> or <paramref name="textName"/> is null.
        /// </exception>
        public RpRecord(ResourceRecordInfo info, DnsString mailbox, DnsString textName)
            : base(info)
        {
            MailboxDomainName = mailbox ?? throw new ArgumentNullException(nameof(mailbox));
            TextDomainName = textName ?? throw new ArgumentNullException(nameof(textName));
        }

        private protected override string RecordToString()
        {
            return $"{MailboxDomainName} {TextDomainName}";
        }
    }
}