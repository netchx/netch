using System;

namespace DNS.Protocol.ResourceRecords
{
    public class NameServerResourceRecord : BaseResourceRecord
    {
        public NameServerResourceRecord(IResourceRecord record, byte[] message, int dataOffset)
            : base(record)
        {
            NSDomainName = Domain.FromArray(message, dataOffset);
        }

        public NameServerResourceRecord(Domain domain, Domain nsDomain, TimeSpan ttl = default(TimeSpan)) :
            base(new ResourceRecord(domain, nsDomain.ToArray(), RecordType.NS, RecordClass.IN, ttl))
        {
            NSDomainName = nsDomain;
        }

        public Domain NSDomainName
        {
            get;
            private set;
        }

        public override string ToString()
        {
            return Stringify().Add("NSDomainName").ToString();
        }
    }
}
