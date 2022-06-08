using Netch.Models;
using Netch.Utils;

namespace Netch.Servers;

public static class VLiteConfigUtils
{
    public static async Task<VLiteConfig> GenerateClientConfigAsync(Server server)
    {
        VLiteServer vlite = (VLiteServer)server;
        var vliteConfig = new VLiteConfig
        {
            inbounds = new object[]
            {
                new
                {
                    port = Global.Settings.Socks5LocalPort,
                    protocol = "socks",
                    settings = new
                    {
                        address = Global.Settings.LocalAddress,
                        udpEnabled = true,
                        packetEncoding = Global.Settings.V2RayConfig.XrayCone ? "Packet" : "None"
                    }
                }
            },
            outbounds = new object[]
            {
                new
                {
                    protocol = "freedom"
                },
                new
                {
                    protocol = "vliteu",
                    settings = new
                    {
                       address = await server.AutoResolveHostnameAsync(),
                       port = server.Port,
                        password = vlite.Password,
                        scramblePacket = Convert.ToBoolean(vlite.ScramblePacket),
                        enableFec = Convert.ToBoolean(vlite.EnableFec),
                        enableStabilization = Convert.ToBoolean(vlite.EnableStabilization),
                        enableRenegotiation = Convert.ToBoolean(vlite.EnableRenegotiation),
                        handshakeMaskingPaddingSize = vlite.HandshakeMaskingPaddingSize
                    },
                    tag = "vlite"
                },
            },
            router = new
            {
                domainStrategy = "AsIs",
                rule = new object[]
                {
                    new
                    {
                        tag = "vlite",
                        domain = Global.Settings.V2RayConfig.SendOnlyUDPTraffic
                        ? new object[]
                        {
                            new
                            {
                                type = "RootDomain",
                                value = "packet-addr.v2fly.arpa"
                            }
                        }
                        : null,
                        portList = Global.Settings.V2RayConfig.SendOnlyUDPTraffic ? null : "0-65535"
                    }
                }
            }
        };

        return vliteConfig;
    }
}