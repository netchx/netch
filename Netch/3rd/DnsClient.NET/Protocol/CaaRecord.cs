using System;
namespace DnsClient.Protocol
{
    /* RFC 6844 (https://tools.ietf.org/html/rfc6844#section-5.1)
    A CAA RR contains a single property entry consisting of a tag-value
    pair.  Each tag represents a property of the CAA record.  The value
    of a CAA property is that specified in the corresponding value field.

    A domain name MAY have multiple CAA RRs associated with it and a
    given property MAY be specified more than once.

    The CAA data field contains one property entry.  A property entry
    consists of the following data fields:

       +0-1-2-3-4-5-6-7-|0-1-2-3-4-5-6-7-|
       | Flags          | Tag Length = n |
       +----------------+----------------+...+---------------+
       | Tag char 0     | Tag char 1     |...| Tag char n-1  |
       +----------------+----------------+...+---------------+
       +----------------+----------------+.....+----------------+
       | Value byte 0   | Value byte 1   |.....| Value byte m-1 |
       +----------------+----------------+.....+----------------+

    Where n is the length specified in the Tag length field and m is the
    remaining octets in the Value field (m = d - n - 2) where d is the
    length of the RDATA section.

    The data fields are defined as follows:

    Flags:  One octet containing the following fields:

      Bit 0, Issuer Critical Flag:  If the value is set to '1', the
         critical flag is asserted and the property MUST be understood
         if the CAA record is to be correctly processed by a certificate
         issuer.

         A Certification Authority MUST NOT issue certificates for any
         Domain that contains a CAA critical property for an unknown or
         unsupported property tag that for which the issuer critical
         flag is set.

      Note that according to the conventions set out in [RFC1035], bit 0
      is the Most Significant Bit and bit 7 is the Least Significant
      Bit. Thus, the Flags value 1 means that bit 7 is set while a value
      of 128 means that bit 0 is set according to this convention.

      All other bit positions are reserved for future use.

      To ensure compatibility with future extensions to CAA, DNS records
      compliant with this version of the CAA specification MUST clear
      (set to "0") all reserved flags bits.  Applications that interpret
      CAA records MUST ignore the value of all reserved flag bits.

    Tag Length:  A single octet containing an unsigned integer specifying
      the tag length in octets.  The tag length MUST be at least 1 and
      SHOULD be no more than 15.

    Tag:  The property identifier, a sequence of US-ASCII characters.

      Tag values MAY contain US-ASCII characters 'a' through 'z', 'A'
      through 'Z', and the numbers 0 through 9.  Tag values SHOULD NOT
      contain any other characters.  Matching of tag values is case
      insensitive.

      Tag values submitted for registration by IANA MUST NOT contain any
      characters other than the (lowercase) US-ASCII characters 'a'
      through 'z' and the numbers 0 through 9.

    Value:  A sequence of octets representing the property value.
      Property values are encoded as binary values and MAY employ sub-
      formats.

      The length of the value field is specified implicitly as the
      remaining length of the enclosing Resource Record data field.
    * */

    /// <summary>
    /// A <see cref="DnsResourceRecord"/> representing a certification authority authorization.
    /// <para>
    /// The Certification Authority Authorization (CAA) DNS Resource Record
    /// allows a DNS domain name holder to specify one or more Certification
    /// Authorities(CAs) authorized to issue certificates for that domain.
    /// </para>
    /// <para>
    /// CAA Resource Records allow a public Certification Authority to
    /// implement additional controls to reduce the risk of unintended
    /// certificate mis-issue.This document defines the syntax of the CAA
    /// record and rules for processing CAA records by certificate issuers.
    /// </para>
    /// </summary>
    /// <seealso href="https://tools.ietf.org/html/rfc6844">RFC 6844</seealso>
    public class CaaRecord : DnsResourceRecord
    {
        /// <summary>
        /// One octet containing the flags.
        /// </summary>
        public byte Flags { get; }

        /// <summary>
        /// The property identifier, a sequence of US-ASCII characters.
        /// </summary>
        public string Tag { get; }

        /// <summary>
        /// A sequence of octets representing the property value.
        /// Property values are encoded as binary values and MAY employ sub-formats.
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CaaRecord"/> class.
        /// </summary>
        /// <param name="info">The information.</param>
        /// <param name="flags">The flags.</param>
        /// <param name="tag">The tag.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="System.ArgumentNullException">If <paramref name="info"/> or <paramref name="tag"/> or <paramref name="value"/> is null.</exception>
        public CaaRecord(ResourceRecordInfo info, byte flags, string tag, string value) : base(info)
        {
            Flags = flags;
            Tag = tag ?? throw new ArgumentNullException(nameof(tag));
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }

        private protected override string RecordToString()
        {
            return $"{Flags} {Tag} \"{Value}\"";
        }
    }
}