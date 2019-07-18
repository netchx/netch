using System;

namespace DnsClient
{
    /// <summary>
    /// Represents a simple request message which can be send through <see cref="DnsMessageHandler"/>.
    /// </summary>
    internal class DnsRequestMessage
    {
        public DnsRequestHeader Header { get; }

        public DnsQuestion Question { get; }

        public DnsRequestMessage(DnsRequestHeader header, DnsQuestion question)
        {
            Header = header ?? throw new ArgumentNullException(nameof(header));
            Question = question ?? throw new ArgumentNullException(nameof(question));
        }
    }
}