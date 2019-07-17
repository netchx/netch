using DNS.Protocol.Utils;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace DNS.Protocol.ResourceRecords
{
    public class ResourceRecord : IResourceRecord
    {
        private Domain domain;
        private RecordType type;
        private RecordClass klass;
        private TimeSpan ttl;
        private byte[] data;

        public static IList<ResourceRecord> GetAllFromArray(byte[] message, int offset, int count)
        {
            return GetAllFromArray(message, offset, count, out offset);
        }

        public static IList<ResourceRecord> GetAllFromArray(byte[] message, int offset, int count, out int endOffset)
        {
            IList<ResourceRecord> records = new List<ResourceRecord>(count);

            for (int i = 0; i < count; i++)
            {
                records.Add(FromArray(message, offset, out offset));
            }

            endOffset = offset;
            return records;
        }

        public static ResourceRecord FromArray(byte[] message, int offset)
        {
            return FromArray(message, offset, out offset);
        }

        public static ResourceRecord FromArray(byte[] message, int offset, out int endOffset)
        {
            Domain domain = Domain.FromArray(message, offset, out offset);
            Tail tail = Marshalling.Struct.GetStruct<Tail>(message, offset, Tail.SIZE);

            byte[] data = new byte[tail.DataLength];

            offset += Tail.SIZE;
            Array.Copy(message, offset, data, 0, data.Length);

            endOffset = offset + data.Length;

            return new ResourceRecord(domain, data, tail.Type, tail.Class, tail.TimeToLive);
        }

        public static ResourceRecord FromQuestion(Question question, byte[] data, TimeSpan ttl = default(TimeSpan))
        {
            return new ResourceRecord(question.Name, data, question.Type, question.Class, ttl);
        }

        public ResourceRecord(Domain domain, byte[] data, RecordType type,
                RecordClass klass = RecordClass.IN, TimeSpan ttl = default(TimeSpan))
        {
            this.domain = domain;
            this.type = type;
            this.klass = klass;
            this.ttl = ttl;
            this.data = data;
        }

        public Domain Name
        {
            get { return domain; }
        }

        public RecordType Type
        {
            get { return type; }
        }

        public RecordClass Class
        {
            get { return klass; }
        }

        public TimeSpan TimeToLive
        {
            get { return ttl; }
        }

        public int DataLength
        {
            get { return data.Length; }
        }

        public byte[] Data
        {
            get { return data; }
        }

        public int Size
        {
            get { return domain.Size + Tail.SIZE + data.Length; }
        }

        public byte[] ToArray()
        {
            ByteStream result = new ByteStream(Size);

            result
                .Append(domain.ToArray())
                .Append(Marshalling.Struct.GetBytes<Tail>(new Tail()
                {
                    Type = Type,
                    Class = Class,
                    TimeToLive = ttl,
                    DataLength = data.Length
                }))
                .Append(data);

            return result.ToArray();
        }

        public override string ToString()
        {
            return ObjectStringifier.New(this)
                .Add("Name", "Type", "Class", "TimeToLive", "DataLength")
                .ToString();
        }

        [Marshalling.Endian(Marshalling.Endianness.Big)]
        [StructLayout(LayoutKind.Sequential, Pack = 2)]
        private struct Tail
        {
            public const int SIZE = 10;

            private ushort type;
            private ushort klass;
            private uint ttl;
            private ushort dataLength;

            public RecordType Type
            {
                get { return (RecordType)type; }
                set { type = (ushort)value; }
            }

            public RecordClass Class
            {
                get { return (RecordClass)klass; }
                set { klass = (ushort)value; }
            }

            public TimeSpan TimeToLive
            {
                get { return TimeSpan.FromSeconds(ttl); }
                set { ttl = (uint)value.TotalSeconds; }
            }

            public int DataLength
            {
                get { return dataLength; }
                set { dataLength = (ushort)value; }
            }
        }
    }
}
