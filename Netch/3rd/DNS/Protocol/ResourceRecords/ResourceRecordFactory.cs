using System.Collections.Generic;

namespace DNS.Protocol.ResourceRecords
{
    public static class ResourceRecordFactory
    {
        public static IList<IResourceRecord> GetAllFromArray(byte[] message, int offset, int count)
        {
            return GetAllFromArray(message, offset, count, out offset);
        }

        public static IList<IResourceRecord> GetAllFromArray(byte[] message, int offset, int count, out int endOffset)
        {
            IList<IResourceRecord> result = new List<IResourceRecord>(count);

            for (int i = 0; i < count; i++)
            {
                result.Add(FromArray(message, offset, out offset));
            }

            endOffset = offset;
            return result;
        }

        public static IResourceRecord FromArray(byte[] message, int offset)
        {
            return FromArray(message, offset, out offset);
        }

        public static IResourceRecord FromArray(byte[] message, int offset, out int endOffest)
        {
            ResourceRecord record = ResourceRecord.FromArray(message, offset, out endOffest);
            int dataOffset = endOffest - record.DataLength;

            switch (record.Type)
            {
                case RecordType.A:
                case RecordType.AAAA:
                    return new IPAddressResourceRecord(record);
                case RecordType.NS:
                    return new NameServerResourceRecord(record, message, dataOffset);
                case RecordType.CNAME:
                    return new CanonicalNameResourceRecord(record, message, dataOffset);
                case RecordType.SOA:
                    return new StartOfAuthorityResourceRecord(record, message, dataOffset);
                case RecordType.PTR:
                    return new PointerResourceRecord(record, message, dataOffset);
                case RecordType.MX:
                    return new MailExchangeResourceRecord(record, message, dataOffset);
                case RecordType.TXT:
                    return new TextResourceRecord(record);
                default:
                    return record;
            }
        }
    }
}
