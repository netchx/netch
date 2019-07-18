using System;
using System.Runtime.InteropServices;

namespace DnsClient
{
    internal class DisposableIntPtr : IDisposable
    {
        public IntPtr Ptr => _ptr;

        public bool IsValid { get; private set; } = true;

        private IntPtr _ptr;

        private DisposableIntPtr()
        {
        }

        public static DisposableIntPtr Alloc(int size)
        {
            var ptr = new DisposableIntPtr();
            try
            {
                ptr._ptr = Marshal.AllocHGlobal(size);
            }
            catch (OutOfMemoryException)
            {
                ptr.IsValid = false;
            }

            return ptr;
        }

        public void Dispose()
        {
            Marshal.FreeHGlobal(_ptr);
        }
    }
}