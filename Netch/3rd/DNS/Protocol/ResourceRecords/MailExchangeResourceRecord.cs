using System;

namespace DNS.Protocol.ResourceRecords
{
    public class MailExchangeResourceRecord : BaseResourceRecord
    {
        private const int PREFERENCE_SIZE = 2;

        private static IResourceRecord Create(Domain domain, int preference, Domain exchange, TimeSpan ttl)
        {
            byte[] pref = BitConverter.GetBytes((ushort)preference);
            byte[] data = new byte[pref.Length + exchange.Size];

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(pref);
            }

            pref.CopyTo(data, 0);
            exchange.ToArray().CopyTo(data, pref.Length);

            return new ResourceRecord(domain, data, RecordType.MX, RecordClass.IN, ttl);
        }

        public MailExchangeResourceRecord(IResourceRecord record, byte[] message, int dataOffset)
            : base(record)
        {
            byte[] preference = new byte[MailExchangeResourceRecord.PREFERENCE_SIZE];
            Array.Copy(message, dataOffset, preference, 0, preference.Length);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(preference);
            }

            dataOffset += MailExchangeResourceRecord.PREFERENCE_SIZE;

            Preference = BitConverter.ToUInt16(preference, 0);
            ExchangeDomainName = Domain.FromArray(message, dataOffset);
        }

        public MailExchangeResourceRecord(Domain domain, int preference, Domain exchange, TimeSpan ttl = default(TimeSpan)) :
            base(Create(domain, preference, exchange, ttl))
        {
            Preference = preference;
            ExchangeDomainName = exchange;
        }

        public int Preference
        {
            get;
            private set;
        }

        public Domain ExchangeDomainName
        {
            get;
            private set;
        }

        public override string ToString()
        {
            return Stringify().Add("Preference", "ExchangeDomainName").ToString();
        }
    }
}
