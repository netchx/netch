using System;
using System.Net;

namespace DnsClient.Protocol
{
    /// <summary>
    /// Base class for <see cref="DnsResourceRecord"/>s transporting an <see cref="IPAddress"/>.
    /// </summary>
    /// <seealso cref="ARecord"/>
    /// <seealso cref="AaaaRecord"/>
    public class AddressRecord : DnsResourceRecord
    {
        /// <summary>
        /// Gets the <see cref="IPAddress"/>.
        /// </summary>
        /// <value>
        /// The address.
        /// </value>
        public IPAddress Address { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AddressRecord"/> class.
        /// </summary>
        /// <param name="info">The information.</param>
        /// <param name="address">The address.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="info"/> is null.</exception>
        /// <exception cref="System.ArgumentNullException">If <paramref name="address"/> or <paramref name="info"/> is null</exception>
        public AddressRecord(ResourceRecordInfo info, IPAddress address)
            : base(info)
        {
            Address = address ?? throw new ArgumentNullException(nameof(address));
        }

        private protected override string RecordToString()
        {
            return Address.ToString();
        }
    }
}