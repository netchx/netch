namespace Netch.Models
{
    public class STUN_Client
    {
        public enum NatType
        {
            UdpBlocked,
            OpenInternet,
            SymmetricUdpFirewall,
            FullCone,
            RestrictedCone,
            PortRestrictedCone,
            Symmetric
        }
    }
}