using System;

namespace DnsClient.Protocol
{
    /* RFC 2782 (https://tools.ietf.org/html/rfc2782)

    The format of the SRV RR

    Here is the format of the SRV RR, whose DNS type code is 33:

        _Service._Proto.Name TTL Class SRV Priority Weight Port Target

        (There is an example near the end of this document.)

    Service
        The symbolic name of the desired service, as defined in Assigned
        Numbers [STD 2] or locally.  An underscore (_) is prepended to
        the service identifier to avoid collisions with DNS labels that
        occur in nature.
        Some widely used services, notably POP, don't have a single
        universal name.  If Assigned Numbers names the service
        indicated, that name is the only name which is legal for SRV
        lookups.  The Service is case insensitive.

    Proto
        The symbolic name of the desired protocol, with an underscore
        (_) prepended to prevent collisions with DNS labels that occur
        in nature.  _TCP and _UDP are at present the most useful values
        for this field, though any name defined by Assigned Numbers or
        locally may be used (as for Service).  The Proto is case
        insensitive.

    Name
        The domain this RR refers to.  The SRV RR is unique in that the
        name one searches for is not this name; the example near the end
        shows this clearly.

    TTL
        Standard DNS meaning [RFC 1035].

    Class
        Standard DNS meaning [RFC 1035].   SRV records occur in the IN
        Class.

    Priority
        The priority of this target host.  A client MUST attempt to
        contact the target host with the lowest-numbered priority it can
        reach; target hosts with the same priority SHOULD be tried in an
        order defined by the weight field.  The range is 0-65535.  This
        is a 16 bit unsigned integer in network byte order.

    Weight
        A server selection mechanism.  The weight field specifies a
        relative weight for entries with the same priority. Larger
        weights SHOULD be given a proportionately higher probability of
        being selected. The range of this number is 0-65535.  This is a
        16 bit unsigned integer in network byte order.  Domain
        administrators SHOULD use Weight 0 when there isn't any server
        selection to do, to make the RR easier to read for humans (less
        noisy).  In the presence of records containing weights greater
        than 0, records with weight 0 should have a very small chance of
        being selected.

        In the absence of a protocol whose specification calls for the
        use of other weighting information, a client arranges the SRV
        RRs of the same Priority in the order in which target hosts,

        specified by the SRV RRs, will be contacted. The following
        algorithm SHOULD be used to order the SRV RRs of the same
        priority:

        To select a target to be contacted next, arrange all SRV RRs
        (that have not been ordered yet) in any order, except that all
        those with weight 0 are placed at the beginning of the list.

        Compute the sum of the weights of those RRs, and with each RR
        associate the running sum in the selected order. Then choose a
        uniform random number between 0 and the sum computed
        (inclusive), and select the RR whose running sum value is the
        first in the selected order which is greater than or equal to
        the random number selected. The target host specified in the
        selected SRV RR is the next one to be contacted by the client.
        Remove this SRV RR from the set of the unordered SRV RRs and
        apply the described algorithm to the unordered SRV RRs to select
        the next target host.  Continue the ordering process until there
        are no unordered SRV RRs.  This process is repeated for each
        Priority.

    Port
        The port on this target host of this service.  The range is 0-
        65535.  This is a 16 bit unsigned integer in network byte order.
        This is often as specified in Assigned Numbers but need not be.

    Target
        The domain name of the target host.  There MUST be one or more
        address records for this name, the name MUST NOT be an alias (in
        the sense of RFC 1034 or RFC 2181).  Implementors are urged, but
        not required, to return the address record(s) in the Additional
        Data section.  Unless and until permitted by future standards
        action, name compression is not to be used for this field.

        A Target of "." means that the service is decidedly not
        available at this domain.

    */

    /// <summary>
    /// A <see cref="DnsResourceRecord"/> represending a location of the server(s) for a specific protocol and domain.
    /// </summary>
    /// <seealso href="https://tools.ietf.org/html/rfc2782">RFC 2782</seealso>
    public class SrvRecord : DnsResourceRecord
    {
        /// <summary>
        /// Gets the port.
        /// </summary>
        /// <value>
        /// The port.
        /// </value>
        public ushort Port { get; }

        /// <summary>
        /// Gets the priority.
        /// </summary>
        /// <value>
        /// The priority.
        /// </value>
        public ushort Priority { get; }

        /// <summary>
        /// Gets the target domain name.
        /// </summary>
        /// <value>
        /// The target.
        /// </value>
        public DnsString Target { get; }

        /// <summary>
        /// Gets the weight.
        /// </summary>
        /// <value>
        /// The weight.
        /// </value>
        public ushort Weight { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SrvRecord" /> class.
        /// </summary>
        /// <param name="info">The information.</param>
        /// <param name="priority">The priority.</param>
        /// <param name="weigth">The weigth.</param>
        /// <param name="port">The port.</param>
        /// <param name="target">The target.</param>
        /// <exception cref="System.ArgumentNullException">If <paramref name="info"/> or <paramref name="target"/> is null.</exception>
        public SrvRecord(ResourceRecordInfo info, ushort priority, ushort weigth, ushort port, DnsString target)
            : base(info)
        {
            Priority = priority;
            Weight = weigth;
            Port = port;
            Target = target ?? throw new ArgumentNullException(nameof(target));
        }

        private protected override string RecordToString()
        {
            return string.Format(
                "{0} {1} {2} {3}",
                Priority,
                Weight,
                Port,
                Target);
        }
    }
}