using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DNS.Protocol.ResourceRecords
{
    public class TextResourceRecord : BaseResourceRecord
    {
        /// Regular expression that matches the attribute name/value.
        /// The first unescaped equal sign is the name/value delimiter.
        private static readonly Regex PATTERN_TXT_RECORD = new Regex(@"^([ -~]*?)(?<!`)=([ -~]*)$");

        /// Regular expression that matches unescaped leading/trailing whitespace.
        private static readonly Regex PATTERN_TRIM_NAME = new Regex(@"^\s+|((?<!`)\s)+$");

        /// Regular expression that matches unescaped characters.
        private static readonly Regex PATTERN_ESCAPE = new Regex(@"([`=])");

        /// Regular expression that matches escaped characters.
        private static readonly Regex PATTERN_UNESCAPE = new Regex(@"`([`=\s])");

        private static string Trim(string value) => PATTERN_TRIM_NAME.Replace(value, string.Empty);
        private static string Escape(string value) => PATTERN_ESCAPE.Replace(value, "`$1");
        private static string Unescape(string value) => PATTERN_UNESCAPE.Replace(value, "$1");

        private static IResourceRecord Create(Domain domain, IList<CharacterString> characterStrings, TimeSpan ttl)
        {
            byte[] data = new byte[characterStrings.Sum(c => c.Size)];
            int offset = 0;

            foreach (CharacterString characterString in characterStrings)
            {
                characterString.ToArray().CopyTo(data, offset);
                offset += characterString.Size;
            }

            return new ResourceRecord(domain, data, RecordType.TXT, RecordClass.IN, ttl);
        }

        private static IList<CharacterString> FormatAttributeNameValue(string attributeName, string attributeValue)
        {
            return CharacterString.FromString($"{Escape(attributeName)}={attributeValue}");
        }

        public TextResourceRecord(IResourceRecord record) :
            base(record)
        {
            TextData = CharacterString.GetAllFromArray(Data, 0);
        }

        public TextResourceRecord(Domain domain, IList<CharacterString> characterStrings,
                TimeSpan ttl = default(TimeSpan)) : base(Create(domain, characterStrings, ttl))
        {
            TextData = new ReadOnlyCollection<CharacterString>(characterStrings);
        }

        public TextResourceRecord(Domain domain, string attributeName, string attributeValue,
                TimeSpan ttl = default(TimeSpan)) :
                this(domain, FormatAttributeNameValue(attributeName, attributeValue), ttl)
        { }

        public IList<CharacterString> TextData
        {
            get;
            private set;
        }

        public KeyValuePair<string, string> Attribute
        {
            get
            {
                string text = ToStringTextData();
                Match match = PATTERN_TXT_RECORD.Match(text);

                if (match.Success)
                {
                    string attributeName = (match.Groups[1].Length > 0) ?
                        Unescape(Trim(match.Groups[1].ToString())) : null;
                    string attributeValue = Unescape(match.Groups[2].ToString());
                    return new KeyValuePair<string, string>(attributeName, attributeValue);
                }
                else
                {
                    return new KeyValuePair<string, string>(null, Unescape(text));
                }
            }
        }

        public string ToStringTextData()
        {
            return ToStringTextData(Encoding.ASCII);
        }

        public string ToStringTextData(Encoding encoding)
        {
            return String.Join(string.Empty, TextData.Select(c => c.ToString(encoding)));
        }

        public override string ToString()
        {
            return Stringify().Add("TextData", (object)ToStringTextData()).ToString();
        }
    }
}
