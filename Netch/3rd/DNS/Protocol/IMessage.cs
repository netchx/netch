using System.Collections.Generic;

namespace DNS.Protocol
{
    public interface IMessage
    {
        IList<Question> Questions { get; }

        int Size { get; }
        byte[] ToArray();
    }
}
