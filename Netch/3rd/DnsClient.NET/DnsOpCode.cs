using System;

namespace DnsClient
{
    /*
     *
     * Reference: [RFC6895][RFC1035]
        0	    Query	                            [RFC1035]
        1	    IQuery (Inverse Query, OBSOLETE)	[RFC3425]
        2	    Status	                            [RFC1035]
        3	    Unassigned
        4	    Notify	                            [RFC1996]
        5	    Update	                            [RFC2136]
        6-15	Unassigned
     * */

    /// <summary>
    /// Specifies kind of query in this message.
    /// This value is set by the originator of a query and copied into the response.
    /// </summary>
    public enum DnsOpCode : short
    {
        /// <summary>
        /// A standard query.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc1035">RFC 1035</seealso>
        Query,

        /// <summary>
        /// An inverse query.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc3425">RFC 3425</seealso>
        [Obsolete]
        IQuery,

        /// <summary>
        /// A server status request.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc1035">RFC 1035</seealso>
        Status,

        /// <summary>
        /// Unassigned value
        /// </summary>
        Unassinged3,

        /// <summary>
        /// Notify query.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc1996">RFC 1996</seealso>
        Notify,

        /// <summary>
        /// Update query.
        /// </summary>
        /// <seealso href="https://tools.ietf.org/html/rfc2136">RFC 2136</seealso>
        Update,

        /// <summary>
        /// Unassigned value
        /// </summary>
        Unassinged6,

        /// <summary>
        /// Unassigned value
        /// </summary>
        Unassinged7,

        /// <summary>
        /// Unassigned value
        /// </summary>
        Unassinged8,

        /// <summary>
        /// Unassigned value
        /// </summary>
        Unassinged9,

        /// <summary>
        /// Unassigned value
        /// </summary>
        Unassinged10,

        /// <summary>
        /// Unassigned value
        /// </summary>
        Unassinged11,

        /// <summary>
        /// Unassigned value
        /// </summary>
        Unassinged12,

        /// <summary>
        /// Unassigned value
        /// </summary>
        Unassinged13,

        /// <summary>
        /// Unassigned value
        /// </summary>
        Unassinged14,

        /// <summary>
        /// Unassigned value
        /// </summary>
        Unassinged15,
    }
}