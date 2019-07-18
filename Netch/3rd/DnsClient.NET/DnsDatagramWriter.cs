using DnsClient.Internal;
using System;
using System.Diagnostics;
using System.Net;
using System.Text;

namespace DnsClient
{
    internal class DnsDatagramWriter : IDisposable
    {
        // queries can only be 255 octets + some header bytes, so that size is pretty safe...
        public const int BufferSize = 1024;

        private const byte DotByte = 46;

        private readonly PooledBytes _pooledBytes;

        private ArraySegment<byte> _buffer;

        public ArraySegment<byte> Data
        {
            get
            {
                return new ArraySegment<byte>(_buffer.Array, 0, Index);
            }
        }

        public int Index { get; set; }

        public DnsDatagramWriter()
        {
            _pooledBytes = new PooledBytes(BufferSize);
            _buffer = new ArraySegment<byte>(_pooledBytes.Buffer, 0, BufferSize);
        }

        public DnsDatagramWriter(ArraySegment<byte> useBuffer)
        {
            Debug.Assert(useBuffer.Count >= BufferSize);

            _buffer = useBuffer;
        }

        public void WriteHostName(string queryName)
        {
            var bytes = Encoding.UTF8.GetBytes(queryName);
            int lastOctet = 0;
            var index = 0;
            if (bytes.Length <= 1)
            {
                WriteByte(0);
                return;
            }
            foreach (var b in bytes)
            {
                if (b == DotByte)
                {
                    WriteByte((byte)(index - lastOctet)); // length
                    WriteBytes(bytes, lastOctet, index - lastOctet);
                    lastOctet = index + 1;
                }

                index++;
            }

            WriteByte(0);
        }

        public void WriteByte(byte b)
        {
            _buffer.Array[_buffer.Offset + Index++] = b;
        }

        public void WriteBytes(byte[] data, int length) => WriteBytes(data, 0, length);

        public void WriteBytes(byte[] data, int dataOffset, int length)
        {
            Buffer.BlockCopy(data, dataOffset, _buffer.Array, _buffer.Offset + Index, length);

            Index += length;
        }

        public void WriteInt16NetworkOrder(short value)
        {
            var bytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(value));
            WriteBytes(bytes, bytes.Length);
        }

        public void WriteInt32NetworkOrder(int value)
        {
            var bytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(value));
            WriteBytes(bytes, bytes.Length);
        }

        public void WriteUInt16NetworkOrder(ushort value) => WriteInt16NetworkOrder((short)value);

        public void WriteUInt32NetworkOrder(uint value) => WriteInt32NetworkOrder((int)value);

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _pooledBytes?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}