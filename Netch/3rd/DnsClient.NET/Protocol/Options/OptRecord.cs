namespace DnsClient.Protocol.Options
{
    /* https://tools.ietf.org/html/rfc6891#section-4.3
    6.1.2.  Wire Format

   An OPT RR has a fixed part and a variable set of options expressed as
   {attribute, value} pairs.  The fixed part holds some DNS metadata,
   and also a small collection of basic extension elements that we
   expect to be so popular that it would be a waste of wire space to
   encode them as {attribute, value} pairs.

   The fixed part of an OPT RR is structured as follows:

       +------------+--------------+------------------------------+
       | Field Name | Field Type   | Description                  |
       +------------+--------------+------------------------------+
       | NAME       | domain name  | MUST be 0 (root domain)      |
       | TYPE       | u_int16_t    | OPT (41)                     |
       | CLASS      | u_int16_t    | requestor's UDP payload size |
       | TTL        | u_int32_t    | extended RCODE and flags     |
       | RDLEN      | u_int16_t    | length of all RDATA          |
       | RDATA      | octet stream | {attribute,value} pairs      |
       +------------+--------------+------------------------------+

    6.1.3.  OPT Record TTL Field Use

   The extended RCODE and flags, which OPT stores in the RR Time to Live
   (TTL) field, are structured as follows:

                  +0 (MSB)                            +1 (LSB)
       +---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+
    0: |         EXTENDED-RCODE        |            VERSION            |
       +---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+
    2: | DO|                           Z                               |
       +---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+

   EXTENDED-RCODE
      Forms the upper 8 bits of extended 12-bit RCODE (together with the
      4 bits defined in [RFC1035].  Note that EXTENDED-RCODE value 0
      indicates that an unextended RCODE is in use (values 0 through
      15).

   VERSION
      Indicates the implementation level of the setter.  Full
      conformance with this specification is indicated by version '0'.
      Requestors are encouraged to set this to the lowest implemented
      level capable of expressing a transaction, to minimise the
      responder and network load of discovering the greatest common
      implementation level between requestor and responder.  A
      requestor's version numbering strategy MAY ideally be a run-time
      configuration option.
      If a responder does not implement the VERSION level of the
      request, then it MUST respond with RCODE=BADVERS.  All responses
      MUST be limited in format to the VERSION level of the request, but
      the VERSION of each response SHOULD be the highest implementation
      level of the responder.  In this way, a requestor will learn the
      implementation level of a responder as a side effect of every
      response, including error responses and including RCODE=BADVERS.
    */

    /// <summary>
    /// A options resource record.
    /// </summary>
    internal class OptRecord : DnsResourceRecord
    {
        private const uint ResponseCodeMask = 0xff000000;
        private const int ResponseCodeShift = 20;
        private const uint VersionMask = 0x00ff0000;
        private const int VersionShift = 16;

        public DnsResponseCode ResponseCodeEx
        {
            get
            {
                return (DnsResponseCode)((InitialTimeToLive & ResponseCodeMask) >> ResponseCodeShift);
            }
            set
            {
                InitialTimeToLive &= (int)~ResponseCodeMask;
                InitialTimeToLive |= (int)(((int)value << ResponseCodeShift) & ResponseCodeMask);
            }
        }

        public short UdpSize
        {
            get { return (short)RecordClass; }
        }

        public byte Version
        {
            get
            {
                return (byte)((InitialTimeToLive & VersionMask) >> VersionShift);
            }
            set
            {
                InitialTimeToLive = (int)((uint)InitialTimeToLive & ~VersionMask);
                InitialTimeToLive |= (int)((value << VersionShift) & VersionMask);
            }
        }

        public bool IsDnsSecOk
        {
            get { return (InitialTimeToLive & 0x8000) != 0; }
            set
            {
                if (value)
                {
                    InitialTimeToLive |= 0x8000;
                }
                else
                {
                    InitialTimeToLive &= 0x7fff;
                }
            }
        }

        public OptRecord(int size = 4096, int version = 0, int length = 0)
            : base(new ResourceRecordInfo(DnsString.RootLabel, ResourceRecordType.OPT, (QueryClass)size, version, length))
        {
        }

        private protected override string RecordToString()
        {
            return $"OPT {RecordClass}.";
        }
    }
}