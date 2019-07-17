namespace DNS.Protocol
{
    public enum RecordType
    {
        A = 1,
        NS = 2,
        CNAME = 5,
        SOA = 6,
        WKS = 11,
        PTR = 12,
        MX = 15,
        TXT = 16,
        AAAA = 28,
        SRV = 33,
        OPT = 41,
        ANY = 255,
    }
}
