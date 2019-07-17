using System;
using System.Net;

namespace DNS.Protocol.ResourceRecords
{
    public class PointerResourceRecord : BaseResourceRecord
    {
        public PointerResourceRecord(IResourceRecord record, byte[] message, int dataOffset)
            : base(record)
        {
            PointerDomainName = Domain.FromArray(message, dataOffset);
        }

        public PointerResourceRecord(IPAddress ip, Domain pointer, TimeSpan ttl = default(TimeSpan)) :
            base(new ResourceRecord(Domain.PointerName(ip), pointer.ToArray(), RecordType.PTR, RecordClass.IN, ttl))
        {
            PointerDomainName = pointer;
        }

        public Domain PointerDomainName
        {
            get;
            private set;
        }

        public override string ToString()
        {
            return Stringify().Add("PointerDomainName").ToString();
        }
    }
}
