#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System.Text;
using System.Threading;

namespace DnsClient.Internal
{
    public class StringBuilderObjectPool
    {
        private readonly StringBuilder[] _items;
        private readonly int _initialCapacity;
        private readonly int _maxPooledCapacity;
        private StringBuilder _fastAccess;

        public static StringBuilderObjectPool Default { get; } = new StringBuilderObjectPool();

        public StringBuilderObjectPool(int pooledItems = 16, int initialCapacity = 200, int maxPooledCapacity = 1024 * 2)
        {
            _items = new StringBuilder[pooledItems];
            _initialCapacity = initialCapacity;
            _maxPooledCapacity = maxPooledCapacity;
            _fastAccess = Create();
        }

        public StringBuilder Get()
        {
            //return Create();
            var found = _fastAccess;

            if (found == null || Interlocked.CompareExchange(ref _fastAccess, null, found) != found)
            {
                for (var i = 0; i < _items.Length; i++)
                {
                    found = _items[i];

                    if (found != null && Interlocked.CompareExchange(ref _items[i], null, found) == found)
                    {
                        return found;
                    }
                }
            }

            return found ?? Create();
        }

        public void Return(StringBuilder value)
        {
            //return;
            if (value == null || value.Capacity > _maxPooledCapacity)
            {
                return;
            }

            value.Clear();

            if (_fastAccess == null && Interlocked.CompareExchange(ref _fastAccess, value, null) == null)
            {
                return;
            }

            for (var i = 0; i < _items.Length; i++)
            {
                if (Interlocked.CompareExchange(ref _items[i], value, null) == null)
                {
                    return;
                }
            }
        }

        private StringBuilder Create()
        {
            return new StringBuilder(_initialCapacity);
        }
    }
}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member