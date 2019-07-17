using System;
using System.Globalization;

namespace DnsClient
{
    /// <summary>
    /// The <see cref="DnsString"/> type is used to normalize and validate domain names and labels.
    /// </summary>
    public class DnsString
    {
        /// <summary>
        /// The ACE prefix indicates that the domain name label contains not normally supported characters and that the label has been endoded.
        /// </summary>
        public const string ACEPrefix = "xn--";

        /// <summary>
        /// The maximum lenght in bytes for one label.
        /// </summary>
        public const int LabelMaxLength = 63;

        /// <summary>
        /// The maximum supported total length in bytes for a domain nanme. The calculation of the actual
        /// bytes this <see cref="DnsString"/> consumes includes all bytes used for to encode it as octet string.
        /// </summary>
        public const int QueryMaxLength = 255;

        /// <summary>
        /// The root label ".".
        /// </summary>
        public static readonly DnsString RootLabel = new DnsString(".", ".");

        internal static readonly IdnMapping IDN = new IdnMapping();
        private const char Dot = '.';
        private const string DotStr = ".";

        /// <summary>
        /// Gets the orginal value.
        /// </summary>
        public string Original { get; }

        /// <summary>
        /// Gets the validated and eventually modified value.
        /// </summary>
        public string Value { get; }

        internal DnsString(string original, string value)
        {
            Original = original;
            Value = value;
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="DnsString"/> to <see cref="System.String"/>.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator string(DnsString name) => name?.Value;

        /// <inheritdoc />
        public override string ToString()
        {
            return Value;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            return obj.ToString().Equals(Value);
        }

        // removed as this is actually the wrong label representation (also, doesn't work if there are escaped \. in the value
        /////// <summary>
        /////// Returns labels representation of the <see cref="Value"/>.
        /////// </summary>
        ////public IReadOnlyList<string> Labels
        ////{
        ////    get
        ////    {
        ////        return Value.Split('.').Reverse().Select(p => p + DotStr).ToArray();
        ////    }
        ////}

        /// <summary>
        /// Parses the given <paramref name="query"/> and validates all labels.
        /// </summary>
        /// <remarks>
        /// An empty string will be interpreted as root label.
        /// </remarks>
        /// <param name="query">A domain name.</param>
        /// <returns>The <see cref="DnsString"/> representing the given <paramref name="query"/>.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="query"/> is null.</exception>
        public static DnsString Parse(string query)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            int charCount = 0;
            int labelCharCount = 0;
            int labelsCount = 0;

            if (query.Length > 1 && query[0] == Dot)
            {
                throw new ArgumentException($"'{query}' is not a legal name, found leading root label.", nameof(query));
            }

            if (query.Length == 0 || (query.Length == 1 && query.Equals(DotStr)))
            {
                return RootLabel;
            }

            for (int index = 0; index < query.Length; index++)
            {
                var c = query[index];
                if (c == Dot)
                {
                    if (labelCharCount > LabelMaxLength)
                    {
                        throw new ArgumentException($"Label '{labelsCount + 1}' is longer than {LabelMaxLength} bytes.", nameof(query));
                    }

                    labelsCount++;
                    labelCharCount = 0;
                }
                else
                {
                    labelCharCount++;
                    charCount++;
                    if (!(c == '-' || c == '_' ||
                        c >= 'a' && c <= 'z' ||
                        c >= 'A' && c <= 'Z' ||
                        c >= '0' && c <= '9'))
                    {
                        try
                        {
                            var result = IDN.GetAscii(query);
                            if (result[result.Length - 1] != Dot)
                            {
                                result += Dot;
                            }

                            return new DnsString(query, result);
                        }
                        catch (Exception ex)
                        {
                            throw new ArgumentException($"'{query}' is not a valid hostname.", nameof(query), ex);
                        }
                    }
                }
            }

            // check rest
            if (labelCharCount > 0)
            {
                labelsCount++;

                // check again label max length
                if (labelCharCount > LabelMaxLength)
                {
                    throw new ArgumentException($"Label '{labelsCount}' is longer than {LabelMaxLength} bytes.", nameof(query));
                }
            }

            // octets length length bit per label + 2(start +end)
            if (charCount + labelsCount + 1 > QueryMaxLength)
            {
                throw new ArgumentException($"Octet length of '{query}' exceeds maximum of {QueryMaxLength} bytes.", nameof(query));
            }

            if (query[query.Length - 1] != Dot)
            {
                return new DnsString(query, query + Dot);
            }

            return new DnsString(query, query);
        }

        /// <summary>
        /// Transforms names with the <see cref="ACEPrefix"/> to the unicode variant and adds a trailing '.' at the end if not present.
        /// The original value will be kept in this instance in case it is needed.
        /// </summary>
        /// <remarks>
        /// The method does not parse the domain name unless it contains a <see cref="ACEPrefix"/>.
        /// </remarks>
        /// <param name="query">The value to check.</param>
        /// <returns>The <see cref="DnsString"/> representation.</returns>
        public static DnsString FromResponseQueryString(string query)
        {
            if (query.Length == 0 || query[query.Length - 1] != Dot)
            {
                query += DotStr;
            }

            if (query.Contains(ACEPrefix))
            {
                var unicode = IDN.GetUnicode(query);
                return new DnsString(unicode, query);
            }

            return new DnsString(query, query);
        }
    }
}