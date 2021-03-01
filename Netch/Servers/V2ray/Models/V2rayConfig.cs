#nullable disable
using System.Collections.Generic;

namespace Netch.Servers.V2ray.Models
{
    public class V2rayConfig
    {
        public List<Inbounds> inbounds { get; } = new();

        public List<Outbounds> outbounds { get; } = new();

        public Routing routing { get; } = new();
    }

    public class Inbounds
    {
        public string tag { get; set; }

        public ushort port { get; set; }

        public string listen { get; set; }

        public string protocol { get; set; }

        public Sniffing sniffing { get; set; }

        public Inboundsettings settings { get; set; }

        public StreamSettings streamSettings { get; set; }
    }

    public class Inboundsettings
    {
        public string auth { get; set; }

        public bool udp { get; set; }

        public string ip { get; set; }

        public string address { get; set; }

        public List<UsersItem> clients { get; set; }

        public string decryption { get; set; }
    }

    public class UsersItem
    {
        public string id { get; set; }

        public int alterId { get; set; }

        public string email { get; set; }

        public string security { get; set; }

        public string encryption { get; set; }

        public string flow { get; set; }
    }

    public class Sniffing
    {
        public bool enabled { get; set; }

        public List<string> destOverride { get; set; }
    }

    public class Outbounds
    {
        public string tag { get; set; }

        public string protocol { get; set; }

        public Outboundsettings settings { get; set; }

        public StreamSettings streamSettings { get; set; }

        public Mux mux { get; set; }
    }

    public class Outboundsettings
    {
        public List<VnextItem> vnext { get; set; }

        public List<ServersItem> servers { get; set; }

        public Response response { get; set; }
    }

    public class VnextItem
    {
        public string address { get; set; }

        public ushort port { get; set; }

        public List<UsersItem> users { get; set; }
    }

    public class ServersItem
    {
        public string email { get; set; }

        public string address { get; set; }

        public string method { get; set; }

        public bool ota { get; set; }

        public string password { get; set; }

        public ushort port { get; set; }

        public int level { get; set; }

        public List<SocksUsersItem> users { get; set; }
    }

    public class SocksUsersItem
    {
        public string user { get; set; }

        public string pass { get; set; }

        public int level { get; set; }
    }

    public class Mux
    {
        public bool enabled { get; set; }

        public int concurrency { get; set; }
    }

    public class Response
    {
        public string type { get; set; }
    }

    public class Dns
    {
        public List<string> servers { get; set; }
    }

    public class RulesItem
    {
        public string type { get; set; }

        public string port { get; set; }

        public List<string> inboundTag { get; set; }

        public string outboundTag { get; set; }

        public List<string> ip { get; set; }

        public List<string> domain { get; set; }
    }

    public class Routing
    {
        public string domainStrategy { get; set; }

        public List<RulesItem> rules { get; } = new();
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
    }

    public class TlsSettings
    {
        public bool allowInsecure { get; set; }

        public string serverName { get; set; }
    }

    public class TcpSettings
    {
        public Header header { get; set; }
    }

    public class Header
    {
        public string type { get; set; }

        public TCPRequest request { get; set; }

        public object response { get; set; }
    }

    public class TCPRequest
    {
        public TCPRequestHeaders headers { get; set; }

        public string method { get; set; } = "GET";

        public string path { get; set; } = "/";

        public string version { get; set; } = "1.1";
    }

    public class TCPRequestHeaders
    {
        //public string User_Agent = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/55.0.2883.75 Safari/537.36";

        public string Accept_Encoding { get; set; } = "gzip, deflate";

        public string Connection { get; set; } = "keep-alive";

        public string Host { get; set; }

        public string Pragma { get; set; } = "no-cache";
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

        public Header header { get; set; }

        public string seed { get; set; }
    }

    public class WsSettings
    {
        public string path { get; set; }

        public Headers headers { get; set; }
    }

    public class Headers
    {
        public string Host { get; set; }
    }

    public class HttpSettings
    {
        public string path { get; set; }

        public List<string> host { get; set; }
    }

    public class QuicSettings
    {
        public string security { get; set; }

        public string key { get; set; }

        public Header header { get; set; }
    }
}