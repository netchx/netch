namespace DnsClient
{
    /* RFC 1035 (https://tools.ietf.org/html/rfc1035#section-3.2.4)
	 * 3.2.4. CLASS values
	 *
        CLASS fields appear in resource records.  The following CLASS mnemonics
        and values are defined:

        IN              1 the Internet

        CS              2 the CSNET class (Obsolete - used only for examples in
                        some obsolete RFCs)

        CH              3 the CHAOS class

        HS              4 Hesiod [Dyer 87]

	 */

    /// <summary>
    /// CLASS fields appear in resource records.
    /// </summary>
    public enum QueryClass : short
    {
        /// <summary>
        /// The Internet.
        /// </summary>
        IN = 1,

        /// <summary>
        /// The CSNET class (Obsolete - used only for examples in some obsolete RFCs).
        /// </summary>
        CS = 2,

        /// <summary>
        /// The CHAOS class.
        /// </summary>
        CH = 3,

        /// <summary>
        /// Hesiod [Dyer 87].
        /// </summary>
        HS = 4
    }
}