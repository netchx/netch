using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace DNS.Protocol.Marshalling
{
    public static class Struct
    {
        private static byte[] ConvertEndian<T>(byte[] data)
        {
            Type type = typeof(T);
            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            EndianAttribute endian = null;

            if (type.GetTypeInfo().IsDefined(typeof(EndianAttribute), false))
            {
                endian = (EndianAttribute)type.GetTypeInfo().GetCustomAttributes(typeof(EndianAttribute), false).First();
            }

            foreach (FieldInfo field in fields)
            {
                if (endian == null && !field.IsDefined(typeof(EndianAttribute), false))
                {
                    continue;
                }

                int offset = Marshal.OffsetOf<T>(field.Name).ToInt32();
#pragma warning disable 618
                int length = Marshal.SizeOf(field.FieldType);
#pragma warning restore 618
                endian = endian ?? (EndianAttribute)field.GetCustomAttributes(typeof(EndianAttribute), false).First();

                if (endian.Endianness == Endianness.Big && BitConverter.IsLittleEndian ||
                        endian.Endianness == Endianness.Little && !BitConverter.IsLittleEndian)
                {
                    Array.Reverse(data, offset, length);
                }
            }

            return data;
        }

        public static T GetStruct<T>(byte[] data) where T : struct
        {
            return GetStruct<T>(data, 0, data.Length);
        }

        public static T GetStruct<T>(byte[] data, int offset, int length) where T : struct
        {
            byte[] buffer = new byte[length];
            Array.Copy(data, offset, buffer, 0, buffer.Length);

            GCHandle handle = GCHandle.Alloc(ConvertEndian<T>(buffer), GCHandleType.Pinned);

            try
            {
                return Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
            }
            finally
            {
                handle.Free();
            }
        }

        public static byte[] GetBytes<T>(T obj) where T : struct
        {
            byte[] data = new byte[Marshal.SizeOf(obj)];
            GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);

            try
            {
                Marshal.StructureToPtr(obj, handle.AddrOfPinnedObject(), false);
                return ConvertEndian<T>(data);
            }
            finally
            {
                handle.Free();
            }
        }
    }
}
