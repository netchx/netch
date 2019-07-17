using System;

namespace DNS.Protocol.ResourceRecords
{
    public class CanonicalNameResourceRecord : BaseResourceRecord
    {
        public CanonicalNameResourceRecord(IResourceRecord record, byte[] message, int dataOffset)
            : base(record)
        {
            CanonicalDomainName = Domain.FromArray(message, dataOffset);
        }

        public CanonicalNameResourceRecord(Domain domain, Domain cname, TimeSpan ttl = default(TimeSpan)) :
            base(new ResourceRecord(domain, cname.ToArray(), RecordType.CNAME, RecordClass.IN, ttl))
        {
            CanonicalDomainName = cname;
        }

        public Domain CanonicalDomainName
        {
            get;
            private set;
        }

        public override string ToString()
        {
            return Stringify().Add("CanonicalDomainName").ToString();
        }
    }
}
