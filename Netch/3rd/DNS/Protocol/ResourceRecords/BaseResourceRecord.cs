using DNS.Protocol.Utils;
using System;

namespace DNS.Protocol.ResourceRecords
{
    public abstract class BaseResourceRecord : IResourceRecord
    {
        private IResourceRecord record;

        public BaseResourceRecord(IResourceRecord record)
        {
            this.record = record;
        }

        public Domain Name
        {
            get { return record.Name; }
        }

        public RecordType Type
        {
            get { return record.Type; }
        }

        public RecordClass Class
        {
            get { return record.Class; }
        }

        public TimeSpan TimeToLive
        {
            get { return record.TimeToLive; }
        }

        public int DataLength
        {
            get { return record.DataLength; }
        }

        public byte[] Data
        {
            get { return record.Data; }
        }

        public int Size
        {
            get { return record.Size; }
        }

        public byte[] ToArray()
        {
            return record.ToArray();
        }

        internal ObjectStringifier Stringify()
        {
            return ObjectStringifier.New(this)
                .Add("Name", "Type", "Class", "TimeToLive", "DataLength");
        }
    }
}
