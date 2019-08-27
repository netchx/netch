using System.Collections.Generic;

namespace Netch.Objects.Information
{
    public class VMess
    {
        public class InboundSettings
        {
            public bool udp = true;
        }

        public class Inbounds
        {
            public string listen = "127.0.0.1";

            public int port = 2801;

            public string protocol = "socks";

            public InboundSettings settings;
        }

        public class User
        {
            public string id;

            public int alterId;

            public string security;
        }

        public class VNext
        {
            public string address;

            public int port;

            public List<User> users;
        }

        public class WSHeaders
        {
            public string Host;
        }

        public class TCPRequestHeaders
        {
            public string Host;

            //public string User_Agent = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/55.0.2883.75 Safari/537.36";

            public string Accept_Encoding = "gzip, deflate";

            public string Connection = "keep-alive";

            public string Pragma = "no-cache";
        }

        public class TCPRequest
        {
            public string version = "1.1";

            public string method = "GET";

            public string path = "/";

            public TCPRequestHeaders headers;
        }

        public class TCPHeaders
        {
            public string type;

            public TCPRequest request;
        }

        public class WebSocketSettings
        {
            public bool connectionReuse = true;

            public string path = "/";

            public WSHeaders headers;
        }

        public class TCPSettings
        {
            public bool connectionReuse = true;

            public TCPHeaders header;
        }

        public class QUICSettings
        {
            public string security;

            public string key;

            public TCPHeaders header;
        }

        public class KCPSettings
        {
            public int mtu = 1350;

            public int tti = 50;

            public int uplinkCapacity = 12;

            public int downlinkCapacity = 100;

            public bool congestion = false;

            public int readBufferSize = 2;

            public int writeBufferSize = 2;

            public TCPHeaders header;
        }

        public class HTTPSettings
        {
            public string host;

            public string path;
        }

        public class TLSSettings
        {
            public bool allowInsecure = true;
        }

        public class OutboundSettings
        {
            public List<VNext> vnext;
        }

        public class OutboundMux
        {
            public bool enabled = true;
        }

        public class StreamSettings
        {
            public string network;

            public string security;

            public TCPSettings tcpSettings;

            public WebSocketSettings wsSettings;

            public KCPSettings kcpSettings;

            public QUICSettings quicSettings;

            public HTTPSettings httpSettings;

            public TLSSettings tlsSettings;
        }

        public class Outbounds
        {
            public string tag = "proxy";

            public string protocol = "vmess";

            public OutboundSettings settings;

            public StreamSettings streamSettings;

            public OutboundMux mux;
        }

        public class RoutingRules
        {
            public string type = "field";

            public List<string> port;

            public string outboundTag;

            public List<string> ip;

            public List<string> domain;
        }

        public class Routing
        {
            public string domainStrategy = "IPIfNonMatch";

            public List<RoutingRules> rules;
        }

        public class Config
        {
            public List<Inbounds> inbounds;

            public List<Outbounds> outbounds;

            public Routing routing;
        }
    }
}
