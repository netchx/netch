using DNS.Protocol.Utils;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace DNS.Protocol
{
    public class Question : IMessageEntry
    {
        public static IList<Question> GetAllFromArray(byte[] message, int offset, int questionCount)
        {
            return GetAllFromArray(message, offset, questionCount, out offset);
        }

        public static IList<Question> GetAllFromArray(byte[] message, int offset, int questionCount, out int endOffset)
        {
            IList<Question> questions = new List<Question>(questionCount);

            for (int i = 0; i < questionCount; i++)
            {
                questions.Add(FromArray(message, offset, out offset));
            }

            endOffset = offset;
            return questions;
        }

        public static Question FromArray(byte[] message, int offset)
        {
            return FromArray(message, offset, out offset);
        }

        public static Question FromArray(byte[] message, int offset, out int endOffset)
        {
            Domain domain = Domain.FromArray(message, offset, out offset);
            Tail tail = Marshalling.Struct.GetStruct<Tail>(message, offset, Tail.SIZE);

            endOffset = offset + Tail.SIZE;

            return new Question(domain, tail.Type, tail.Class);
        }

        private Domain domain;
        private RecordType type;
        private RecordClass klass;

        public Question(Domain domain, RecordType type = RecordType.A, RecordClass klass = RecordClass.IN)
        {
            this.domain = domain;
            this.type = type;
            this.klass = klass;
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

        public int Size
        {
            get { return domain.Size + Tail.SIZE; }
        }

        public byte[] ToArray()
        {
            ByteStream result = new ByteStream(Size);

            result
                .Append(domain.ToArray())
                .Append(Marshalling.Struct.GetBytes(new Tail { Type = Type, Class = Class }));

            return result.ToArray();
        }

        public override string ToString()
        {
            return ObjectStringifier.New(this)
                .Add("Name", "Type", "Class")
                .ToString();
        }

        [Marshalling.Endian(Marshalling.Endianness.Big)]
        [StructLayout(LayoutKind.Sequential, Pack = 2)]
        private struct Tail
        {
            public const int SIZE = 4;

            private ushort type;
            private ushort klass;

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
        }
    }
}
