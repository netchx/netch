using System;

namespace DnsClient.Protocol
{
    /*
    3.3.13. SOA RDATA format

	    +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
	    /                     MNAME                     /
	    /                                               /
	    +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
	    /                     RNAME                     /
	    +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
	    |                    SERIAL                     |
	    |                                               |
	    +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
	    |                    REFRESH                    |
	    |                                               |
	    +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
	    |                     RETRY                     |
	    |                                               |
	    +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
	    |                    EXPIRE                     |
	    |                                               |
	    +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
	    |                    MINIMUM                    |
	    |                                               |
	    +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+

    where:

    MNAME           The <domain-name> of the name server that was the
				    original or primary source of data for this zone.

    RNAME           A <domain-name> which specifies the mailbox of the
				    person responsible for this zone.

    SERIAL          The unsigned 32 bit version number of the original copy
				    of the zone.  Zone transfers preserve this value.  This
				    value wraps and should be compared using sequence space
				    arithmetic.

    REFRESH         A 32 bit time interval before the zone should be
				    refreshed.

    RETRY           A 32 bit time interval that should elapse before a
				    failed refresh should be retried.

    EXPIRE          A 32 bit time value that specifies the upper limit on
				    the time interval that can elapse before the zone is no
				    longer authoritative.

    MINIMUM         The unsigned 32 bit minimum TTL field that should be
				    exported with any RR from this zone.

    SOA records cause no additional section processing.

    All times are in units of seconds.

    Most of these fields are pertinent only for name server maintenance
    operations.  However, MINIMUM is used in all query operations that
    retrieve RRs from a zone.  Whenever a RR is sent in a response to a
    query, the TTL field is set to the maximum of the TTL field from the RR
    and the MINIMUM field in the appropriate SOA.  Thus MINIMUM is a lower
    bound on the TTL field for all RRs in a zone.  Note that this use of
    MINIMUM should occur when the RRs are copied into the response and not
    when the zone is loaded from a master file or via a zone transfer.  The
    reason for this provison is to allow future dynamic update facilities to
    change the SOA RR with known semantics.
    */

    /// <summary>
    /// A <see cref="DnsResourceRecord"/> represending a SOA (Start Of Authority) record.
    /// </summary>
    /// <seealso href="https://tools.ietf.org/html/rfc1035#section-3.3.13">RFC 1035</seealso>
    public class SoaRecord : DnsResourceRecord
    {
        /// <summary>
        /// Gets a 32 bit time value that specifies the upper limit on
        /// the time interval that can elapse before the zone is no
        /// longer authoritative.
        /// </summary>
        /// <value>
        /// The expiration.
        /// </value>
        public uint Expire { get; }

        /// <summary>
        /// Gets the unsigned 32 bit minimum TTL field that should be
        /// exported with any RR from this zone.
        /// </summary>
        /// <value>
        /// The minimum TTL.
        /// </value>
        public uint Minimum { get; }

        /// <summary>
        /// Gets the domain name of the name server that was the original or primary source of data for this zone.
        /// </summary>
        /// <value>
        /// The doman name.
        /// </value>
        public DnsString MName { get; }

        /// <summary>
        /// Gets a 32 bit time interval before the zone should be refreshed.
        /// </summary>
        /// <value>
        /// The refresh time.
        /// </value>
        public uint Refresh { get; }

        /// <summary>
        /// Gets a 32 bit time interval that should elapse before a failed refresh should be retried.
        /// </summary>
        /// <value>
        /// The retry time.
        /// </value>
        public uint Retry { get; }

        /// <summary>
        /// Gets a domain name which specifies the mailbox of the person responsible for this zone.
        /// </summary>
        /// <value>
        /// The responsible mailbox domain name.
        /// </value>
        public DnsString RName { get; }

        /// <summary>
        /// Gets the unsigned 32 bit version number of the original copy
        /// of the zone.Zone transfers preserve this value. This value wraps and should be compared using sequence space arithmetic.
        /// </summary>
        /// <value>
        /// The serial number.
        /// </value>
        public uint Serial { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SoaRecord" /> class.
        /// </summary>
        /// <param name="info">The information.</param>
        /// <param name="mName">Name original domain name.</param>
        /// <param name="rName">Name responsible domain name.</param>
        /// <param name="serial">The serial number.</param>
        /// <param name="refresh">The refresh time.</param>
        /// <param name="retry">The retry time.</param>
        /// <param name="expire">The expire time.</param>
        /// <param name="minimum">The minimum TTL.</param>
        /// <exception cref="System.ArgumentNullException">
        /// If <paramref name="info"/> or <paramref name="mName"/> or <paramref name="rName"/> is null.
        /// </exception>
        public SoaRecord(ResourceRecordInfo info, DnsString mName, DnsString rName, uint serial, uint refresh, uint retry, uint expire, uint minimum)
            : base(info)
        {
            MName = mName ?? throw new ArgumentNullException(nameof(mName));
            RName = rName ?? throw new ArgumentNullException(nameof(rName));
            Serial = serial;
            Refresh = refresh;
            Retry = retry;
            Expire = expire;
            Minimum = minimum;
        }

        private protected override string RecordToString()
        {
            return string.Format(
                "{0} {1} {2} {3} {4} {5} {6}",
                MName,
                RName,
                Serial,
                Refresh,
                Retry,
                Expire,
                Minimum);
        }
    }
}