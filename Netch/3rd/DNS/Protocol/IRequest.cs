using DNS.Protocol.ResourceRecords;
using System.Collections.Generic;

namespace DNS.Protocol
{
    public interface IRequest : IMessage
    {
        int Id { get; set; }
        IList<IResourceRecord> AdditionalRecords { get; }
        OperationCode OperationCode { get; set; }
        bool RecursionDesired { get; set; }
    }
}
