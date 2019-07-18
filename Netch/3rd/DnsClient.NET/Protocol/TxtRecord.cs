using System;
using System.Collections.Generic;
using System.Linq;

namespace DnsClient.Protocol
{
    /*
    * RFC 1464  https://tools.ietf.org/html/rfc1464

    https://tools.ietf.org/html/rfc1035#section-3.3:
    <character-string> is a single
    length octet followed by that number of characters.  <character-string>
    is treated as binary information, and can be up to 256 characters in
    length (including the length octet).

    https://tools.ietf.org/html/rfc1035#section-3.3.14:
    TXT RDATA format

        +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
        /                   TXT-DATA                    /
        +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+

    where:

    TXT-DATA        One or more <character-string>s.

    TXT RRs are used to hold descriptive text.  The semantics of the text
    depends on the domain where it is found.
    */

    /// <summary>
    /// A <see cref="DnsResourceRecord"/> represending a text resource.
    /// <para>
    /// TXT RRs are used to hold descriptive text. The semantics of the text
    /// depends on the domain where it is found.
    /// </para>
    /// </summary>
    /// <seealso href="https://tools.ietf.org/html/rfc1035#section-3.3">RFC 1035</seealso>
    /// <seealso href="https://tools.ietf.org/html/rfc1464">RFC 1464</seealso>
    public class TxtRecord : DnsResourceRecord
    {
        /// <summary>
        /// Gets the list of TXT values of this resource record in escaped form, valid for root file.
        /// </summary>
        /// <remarks>
        /// See https://tools.ietf.org/html/rfc1035#section-5.1 for escape details.
        /// </remarks>
        public ICollection<string> EscapedText { get; }

        /// <summary>
        /// Gets the actual <c>UTF8</c> representation of the text values of this record.
        /// </summary>
        public ICollection<string> Text { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TxtRecord"/> class.
        /// </summary>
        /// <param name="info">The information.</param>
        /// <param name="values">The values.</param>
        /// <param name="utf8Values">The UTF8 values.</param>
        /// <exception cref="System.ArgumentNullException">
        /// If <paramref name="info"/> or <paramref name="utf8Values"/> or <paramref name="values"/> is null.
        /// </exception>
        public TxtRecord(ResourceRecordInfo info, string[] values, string[] utf8Values)
            : base(info)
        {
            EscapedText = values ?? throw new ArgumentNullException(nameof(values));
            Text = utf8Values ?? throw new ArgumentNullException(nameof(utf8Values));
        }

        private protected override string RecordToString()
        {
            return string.Join(" ", EscapedText.Select(p => "\"" + p + "\"")).Trim();
        }
    }
}