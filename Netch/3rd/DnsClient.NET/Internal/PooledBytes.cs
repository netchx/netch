using System;
using System.Buffers;

namespace DnsClient.Internal
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    public class PooledBytes : IDisposable
    {
        private static readonly ArrayPool<byte> _pool = ArrayPool<byte>.Create(4096 * 4, 100);
        private int _length;
        private ArraySegment<byte> _buffer;
        private bool _disposed = false;

        public PooledBytes(int length)
        {
            if (length <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            _length = length;
            _buffer = new ArraySegment<byte>(_pool.Rent(length), 0, _length);
        }

        public void Extend(int length)
        {
            var newBuffer = _pool.Rent(_length + length);

            System.Buffer.BlockCopy(_buffer.Array, 0, newBuffer, 0, _length);
            _pool.Return(_buffer.Array);
            _length = _length + length;
            _buffer = new ArraySegment<byte>(newBuffer, 0, _length);
        }

        public byte[] Buffer
        {
            get
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(PooledBytes));
                }

                return _buffer.Array;
            }
        }

        public ArraySegment<byte> BufferSegment
        {
            get
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(PooledBytes));
                }

                return _buffer;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;
                _pool.Return(_buffer.Array);
            }
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}