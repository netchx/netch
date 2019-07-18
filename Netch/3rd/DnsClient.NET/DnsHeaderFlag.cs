using System;

namespace DnsClient.Protocol
{
    /* Reference: https://tools.ietf.org/html/rfc6895#section-2
     * Response header fields
     *
                                            1  1  1  1  1  1
              0  1  2  3  4  5  6  7  8  9  0  1  2  3  4  5
             +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
             |                      ID                       |
             +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
       ==>   |QR|   OpCode  |AA|TC|RD|RA| Z|AD|CD|   RCODE   |  <==
             +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
             |                QDCOUNT/ZOCOUNT                |
             +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
             |                ANCOUNT/PRCOUNT                |
             +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
             |                NSCOUNT/UPCOUNT                |
             +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
             |                    ARCOUNT                    |
             +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+

     * */

    /// <summary>
    /// The flags of the header's second 16bit value
    /// </summary>
    [Flags]
    internal enum DnsHeaderFlag : ushort
    {
        IsCheckingDisabled = 0x0010,
        IsAuthenticData = 0x0020,
        FutureUse = 0x0040,             // Z bit seems to be ignored now, see https://tools.ietf.org/html/rfc6895#section-2.1
        RecursionAvailable = 0x0080,
        RecursionDesired = 0x0100,
        ResultTruncated = 0x0200,
        HasAuthorityAnswer = 0x0400,
        HasQuery = 0x8000,
    }

    internal static class DnsHeader
    {
        public static readonly ushort OPCodeMask = 0x7800;
        public static readonly ushort OPCodeShift = 11;
        public static readonly ushort RCodeMask = 0x000F;
    }
}