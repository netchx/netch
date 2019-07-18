using DnsClient.Internal;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace DnsClient
{
    internal class DnsDatagramReader
    {
        public const int IPv6Length = 16;
        public const int IPv4Length = 4;
        private const byte ReferenceByte = 0xc0;
        private const string ACEPrefix = "xn--";

        private readonly ArraySegment<byte> _data;
        private readonly int _count;
        private int _index;

        public int Index
        {
            get
            {
                return _index;
            }
            set
            {
                if (value < 0 || value > _count)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                _index = value;
            }
        }

        public bool DataAvailable => _count - _data.Offset > 0 && _index < _count;

        public DnsDatagramReader(ArraySegment<byte> data, int startIndex = 0)
        {
            _data = data;
            _count = data.Count;
            Index = startIndex;
        }

        public string ReadString()
        {
            var length = ReadByte();

            var result = Encoding.ASCII.GetString(_data.Array, _data.Offset + _index, length);
            _index += length;
            return result;
        }

        public string ReadString(int length)
        {
            var result = Encoding.ASCII.GetString(_data.Array, _data.Offset + _index, length);
            _index += length;
            return result;
        }

        /// <summary>
        /// As defined in https://tools.ietf.org/html/rfc1035#section-5.1 except '()' or '@' or '.'
        /// </summary>
        public static string ParseString(ArraySegment<byte> data)
        {
            var result = ParseString(new DnsDatagramReader(data, 0), data.Count);
            return result;
        }

        public static string ParseString(DnsDatagramReader reader, int length)
        {
            var builder = StringBuilderObjectPool.Default.Get();
            for (var i = 0; i < length; i++)
            {
                byte b = reader.ReadByte();
                char c = (char)b;

                if (b < 32 || b > 126)
                {
                    builder.Append("\\" + b.ToString("000"));
                }
                else if (c == ';')
                {
                    builder.Append("\\;");
                }
                else if (c == '\\')
                {
                    builder.Append("\\\\");
                }
                else if (c == '"')
                {
                    builder.Append("\\\"");
                }
                else
                {
                    builder.Append(c);
                }
            }

            var value = builder.ToString();
            StringBuilderObjectPool.Default.Return(builder);
            return value;
        }

        public static string ReadUTF8String(ArraySegment<byte> data)
        {
            return Encoding.UTF8.GetString(data.Array, data.Offset, data.Count);
        }

        public byte ReadByte()
        {
            try
            {
                return _data.Array[_data.Offset + _index++];
            }
            catch (IndexOutOfRangeException)
            {
                throw new IndexOutOfRangeException($"Cannot read byte {_index}, out of range.");
            }
        }

        public ArraySegment<byte> ReadBytes(int length)
        {
            try
            {
                var result = new ArraySegment<byte>(_data.Array, _data.Offset + _index, length);
                _index += length;
                return result;
            }
            catch (ArgumentException)
            {
                throw new IndexOutOfRangeException($"Cannot read that many bytes: '{length}'.");
            }
        }

        private readonly byte[] _ipV4Buffer = new byte[4];
        private readonly byte[] _ipV6Buffer = new byte[16];

        public IPAddress ReadIPAddress()
        {
            try
            {
                _ipV4Buffer[0] = _data.Array[_data.Offset + _index];
                _ipV4Buffer[1] = _data.Array[_data.Offset + _index + 1];
                _ipV4Buffer[2] = _data.Array[_data.Offset + _index + 2];
                _ipV4Buffer[3] = _data.Array[_data.Offset + _index + 3];

                Index += IPv4Length;
                return new IPAddress(_ipV4Buffer);
            }
            catch (ArgumentOutOfRangeException)
            {
                throw new IndexOutOfRangeException($"Reading IPv4 address, expected {IPv4Length} bytes.");
            }
        }

        public IPAddress ReadIPv6Address()
        {
            try
            {
                for (var i = 0; i < IPv6Length; i++)
                {
                    _ipV6Buffer[i] = _data.Array[_data.Offset + _index + i];
                }

                Index += IPv6Length;
                return new IPAddress(_ipV6Buffer);
            }
            catch (ArgumentOutOfRangeException)
            {
                throw new IndexOutOfRangeException($"Reading IPv6 address, expected {IPv6Length} bytes.");
            }
        }

        public void Advance(int length)
        {
            Index += length;
        }

        public DnsString ReadDnsName()
        {
            var builder = StringBuilderObjectPool.Default.Get();
            var original = StringBuilderObjectPool.Default.Get();
            foreach (var labelArray in ReadLabels())
            {
                foreach (var b in labelArray)
                {
                    char c = (char)b;

                    if (b < 32 || b > 126)
                    {
                        builder.Append("\\" + b.ToString("000"));
                    }
                    else if (c == ';')
                    {
                        builder.Append("\\;");
                    }
                    else if (c == '\\')
                    {
                        builder.Append("\\\\");
                    }
                    else if (c == '"')
                    {
                        builder.Append("\\\"");
                    }
                    else
                    {
                        builder.Append(c);
                    }
                }

                builder.Append(".");

                var label = Encoding.UTF8.GetString(labelArray.Array, labelArray.Offset, labelArray.Count);
                if (label.StartsWith(ACEPrefix, StringComparison.Ordinal))
                {
                    try
                    {
                        label = DnsString.IDN.GetUnicode(label);
                    }
                    catch { /* just do nothing in case the IDN is invalid, better to return something at least */ }
                }

                original.Append(label);
                original.Append(".");
            }

            var value = builder.ToString();
            if (value.Length == 0 || value[value.Length - 1] != '.')
            {
                value += '.';
            }

            var orig = original.ToString();
            if (orig.Length == 0 || orig[orig.Length - 1] != '.')
            {
                orig += '.';
            }

            StringBuilderObjectPool.Default.Return(builder);
            StringBuilderObjectPool.Default.Return(original);

            return new DnsString(orig, value);
        }

        // only used by the DnsQuestion as we don't expect any escaped chars in the actual query posted to and send back from the DNS Server (not supported).
        public DnsString ReadQuestionQueryString()
        {
            var result = StringBuilderObjectPool.Default.Get();
            foreach (var labelArray in ReadLabels())
            {
                var label = Encoding.UTF8.GetString(labelArray.Array, labelArray.Offset, labelArray.Count);
                result.Append(label);
                result.Append(".");
            }

            string value = result.ToString();
            StringBuilderObjectPool.Default.Return(result);
            return DnsString.FromResponseQueryString(value);
        }

        public ICollection<ArraySegment<byte>> ReadLabels()
        {
            var result = new List<ArraySegment<byte>>(10);

            // read the length byte for the label, then get the content from offset+1 to length
            // proceed till we reach zero length byte.
            byte length;
            while ((length = ReadByte()) != 0)
            {
                // respect the reference bit and lookup the name at the given position
                // the reader will advance only for the 2 bytes read.
                if ((length & ReferenceByte) != 0)
                {
                    int subIndex = (length & 0x3f) << 8 | ReadByte();
                    if (subIndex >= _data.Array.Length - 1)
                    {
                        // invalid length pointer, seems to be actual length of a label which exceeds 63 chars...
                        // get back one and continue other labels
                        Index--;
                        result.Add(_data.SubArray(_index, length));
                        Index += length;
                        continue;
                    }

                    var subReader = new DnsDatagramReader(_data.SubArrayFromOriginal(subIndex));
                    var newLabels = subReader.ReadLabels();
                    result.AddRange(newLabels); // add range actually much faster than Concat and equal to or faster than foreach.. (array copy would work maybe)
                    return result;
                }

                if (Index + length >= _count)
                {
                    throw new IndexOutOfRangeException(
                        $"Found invalid label position '{Index - 1}' with length '{length}' in the source data.");
                }

                var label = _data.SubArray(_index, length);

                // maybe store orignial bytes in this instance too?
                result.Add(label);

                Index += length;
            }

            return result;
        }

        public ushort ReadUInt16()
        {
            if (_count < _index + 2)
            {
                throw new IndexOutOfRangeException("Cannot read more data.");
            }

            var result = BitConverter.ToUInt16(_data.Array, _data.Offset + _index);
            _index += 2;
            return result;
        }

        public ushort ReadUInt16NetworkOrder()
        {
            if (_count < _index + 2)
            {
                throw new IndexOutOfRangeException("Cannot read more data.");
            }

            return (ushort)(ReadByte() << 8 | ReadByte());
        }

        public uint ReadUInt32NetworkOrder()
        {
            return (uint)(ReadUInt16NetworkOrder() << 16 | ReadUInt16NetworkOrder());
        }
    }

    internal static class ArraySegmentExtensions
    {
        public static ArraySegment<T> SubArray<T>(this ArraySegment<T> array, int startIndex, int length)
        {
            return new ArraySegment<T>(array.Array, array.Offset + startIndex, length);
        }

        public static ArraySegment<T> SubArrayFromOriginal<T>(this ArraySegment<T> array, int startIndex)
        {
            return new ArraySegment<T>(array.Array, startIndex, array.Array.Length - startIndex);
        }
    }
}