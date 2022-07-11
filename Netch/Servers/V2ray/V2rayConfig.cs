#nullable disable
// ReSharper disable InconsistentNaming

namespace Netch.Servers;

public struct V2rayConfig
{
    public object[] inbounds { get; set; }

    public Outbound[] outbounds { get; set; }
}

public class User
{
    public string id { get; set; }

    public int alterId { get; set; }

    public string security { get; set; }

    public string encryption { get; set; }

    public string flow { get; set; }
}

public class Outbound
{
    public string protocol { get; set; }

    public OutboundConfiguration settings { get; set; }

    public StreamSettings? streamSettings { get; set; }

    public Mux? mux { get; set; }
}

public class OutboundConfiguration
{
    public VnextItem[] vnext { get; set; }

    public object[] servers { get; set; }

    public string version { get; set; }

    public string address { get; set; }

    public string user { get; set; }

    public ushort port { get; set; }

    public string password { get; set; }

    public string packetEncoding { get; set; }

    public string plugin { get; set; }

    public string pluginOpts { get; set; }

    public string[] pluginArgs { get; set; }

    public string[] localAddresses { get; set; }

    public string peerPublicKey { get; set; }

    public string publicKey { get; set; }

    public string privateKey { get; set; }

    public string preSharedKey { get; set; }

    public int mtu { get; set; }
}

public class VnextItem
{
    public string address { get; set; }

    public ushort port { get; set; }

    public User[] users { get; set; }
}

public class ShadowsocksServerItem
{
    public string address { get; set; }
    
    public ushort port { get; set; }

    public string method { get; set; }

    public string password { get; set; }

    public string flow { get; set; }
 }

public class Mux
{
    public bool enabled { get; set; }

    public string packetEncoding { get; set; }

    public int concurrency { get; set; }
}

public class StreamSettings
{
    public string network { get; set; }

    public string security { get; set; }

    public TlsSettings tlsSettings { get; set; }

    public TcpSettings tcpSettings { get; set; }

    public KcpSettings kcpSettings { get; set; }

    public WsSettings wsSettings { get; set; }

    public HttpSettings httpSettings { get; set; }

    public QuicSettings quicSettings { get; set; }

    public TlsSettings xtlsSettings { get; set; }

    public GrpcSettings grpcSettings { get; set; }

    public Sockopt sockopt { get; set; }
}

#region Transport

public class TlsSettings
{
    public bool allowInsecure { get; set; }

    public string serverName { get; set; }
}

public class TcpSettings
{
    public object header { get; set; }
}

public class WsSettings
{
    public string path { get; set; }

    public object headers { get; set; }
}

public class KcpSettings
{
    public int mtu { get; set; }

    public int tti { get; set; }

    public int uplinkCapacity { get; set; }

    public int downlinkCapacity { get; set; }

    public bool congestion { get; set; }

    public int readBufferSize { get; set; }

    public int writeBufferSize { get; set; }

    public object header { get; set; }

    public string seed { get; set; }
}

public class HttpSettings
{
    public string path { get; set; }

    public string[] host { get; set; }
}

public class QuicSettings
{
    public string security { get; set; }

    public string key { get; set; }

    public object header { get; set; }
}

public class GrpcSettings
{
    public string serviceName { get; set; }

    public bool multiMode { get; set; }
}

public class Sockopt
{
    public bool tcpFastOpen { get; set; }
}

#endregion