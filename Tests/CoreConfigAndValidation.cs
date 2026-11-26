using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Netch.Enums;
using Netch.Models;
using Netch.Servers;
using Netch.Utils;

namespace Tests;

[TestClass]
public class CoreConfigAndValidation
{
    private const string Uuid = "ac7f559b-d6d0-4bdc-96b1-5e0bd260779f";
    private static readonly string WgKey = Convert.ToBase64String(Enumerable.Range(1, 32).Select(n => (byte)n).ToArray());
    private static readonly string Ss16Key = Convert.ToBase64String(Enumerable.Range(1, 16).Select(n => (byte)n).ToArray());
    private static readonly Lazy<string> SelfSignedPem = new(CreateSelfSignedPem);

    private Setting _savedSettings;

    [TestInitialize]
    public void ResetSettings()
    {
        _savedSettings = Netch.Global.Settings;
        Netch.Global.Settings = new Setting();
    }

    [TestCleanup]
    public void RestoreSettings()
    {
        Netch.Global.Settings = _savedSettings;
    }

    [TestMethod]
    public void ServerValidatorAcceptsValidNodesForAllThirteenProtocols()
    {
        var pem = SelfSignedPem.Value;
        Assert.IsTrue(ServerValidator.IsValidPemChain(pem));

        var validNodes = new Server[]
        {
            new VLESSServer
            {
                Address = "vless.example", Port = 443, Password = Uuid,
                Network = "xhttp", HeaderType = "packet-up", StreamSecurity = "reality",
                PublicKey = "pub", ShortId = "0123abcd", Extra = """{"xPaddingBytes":"100"}""",
                ProtoExtra = { VlessEncryption = "mlkem768x25519plus.native.0rtt.sample" }
            },
            new VMessServer
            {
                Address = "vmess.example", Port = 443, Password = Uuid,
                Network = "ws", StreamSecurity = "tls", Cert = pem,
                ProtoExtra = { AlterId = 0, VmessSecurity = "auto" }
            },
            new TrojanServer
            {
                Address = "trojan.example", Port = 443, Password = "pw",
                Network = "grpc", StreamSecurity = "reality", PublicKey = "pub"
            },
            new ShadowsocksServer
            {
                Address = "ss.example", Port = 443, Password = $"{Ss16Key}:{Ss16Key}",
                ProtoExtra = { SsMethod = "2022-blake3-aes-128-gcm" }
            },
            new ShadowsocksServer
            {
                Address = "ss-plain.example", Port = 443, Password = "",
                ProtoExtra = { SsMethod = "none" }
            },
            new ShadowsocksRServer
            {
                Address = "ssr.example", Port = 443, Password = "pw",
                EncryptMethod = "aes-256-cfb", Protocol = "origin", OBFS = "plain"
            },
            new SocksServer("socks.example", 1080),
            new SocksServer("socks-auth.example", 1080, "user", "pass"),
            new HttpServer("http.example", 443, "user", "pass")
            {
                StreamSecurity = "tls", Cert = pem,
                ProtoExtra = { HttpHeaders = "Cookie: a=1\nX-Test: val" }
            },
            new Hysteria2Server
            {
                Address = "hy2.example", Port = 443, Password = "pw",
                StreamSecurity = "tls", Cert = pem,
                ProtoExtra =
                {
                    Obfs = "gecko", SalamanderPass = "obfs-pw",
                    GeckoMinPacketSize = "128", GeckoMaxPacketSize = "512",
                    Ports = "443,5000-6000", HopInterval = "10", UpMbps = 50, DownMbps = 100
                }
            },
            new TUICServer
            {
                Address = "tuic.example", Port = 443, Username = Uuid, Password = "pw",
                Cert = pem, ProtoExtra = { CongestionControl = "bbr", TuicUdpRelayMode = "quic" }
            },
            new AnyTLSServer
            {
                Address = "anytls.example", Port = 443, Password = "pw",
                Cert = pem, ProtoExtra =
                {
                    AnyTlsIdleSessionCheckIntervalSeconds = 0,
                    AnyTlsIdleSessionTimeoutSeconds = 30,
                    AnyTlsMinIdleSession = 1
                }
            },
            new WireGuardServer
            {
                Address = "wg.example", Port = 51820, Password = WgKey,
                ProtoExtra =
                {
                    WgPublicKey = WgKey, WgPresharedKey = WgKey,
                    WgInterfaceAddress = "10.0.0.2/32,fd00::2/128",
                    WgReserved = "1,2,3", WgMtu = 1408
                }
            },
            new NaiveServer
            {
                Address = "naive.example", Port = 443, Username = "u", Password = "p",
                ProtoExtra = { InsecureConcurrency = 2, NaiveQuic = true, CongestionControl = "bbr2" }
            },
            new MASQUEServer
            {
                Address = "masque.example", Port = 443,
                Cert = pem, ProtoExtra =
                {
                    MasquePath = "/.well-known/masque/ip/{target}/{ipproto}/",
                    MasqueHeaders = "X-Test: 1"
                }
            },
            new TrustTunnelServer
            {
                Address = "tt.example", Port = 443, Username = "u", Password = "p",
                Cert = pem, EchConfigList = "AQIDBA==",
                ProtoExtra = { TrustTunnelQuic = true, CongestionControl = "bbr", BbrProfile = "standard", TrustTunnelHealthCheck = true, TrustTunnelMaxConnections = 4 }
            },
            new MieruServer
            {
                Address = "mieru.example", Port = 2010, Username = "u", Password = "p",
                ProtoExtra = { MieruTransport = "UDP", MieruPortRange = "2010-2020", MieruMultiplexing = "MULTIPLEXING_LOW", MieruHandshakeMode = "HANDSHAKE_NO_WAIT", MieruTrafficPattern = Ss16Key }
            },
            new ShadowQUICServer
            {
                Address = "sq.example", Port = 443, Username = "u", Password = "p",
                Sni = "sq.example", Alpn = "h3",
                ProtoExtra = { ShadowQuicVersions = "v1,v2", CongestionControl = "bbr", BbrProfile = "aggressive", UpMbps = 50, DownMbps = 100 }
            },
            new AmneziaWGServer
            {
                Address = "awg.example", Port = 51820, Password = WgKey,
                ProtoExtra =
                {
                    WgPublicKey = WgKey, WgInterfaceAddress = "10.0.0.2/32,fd00::2/128",
                    AwgVersion = 3, AwgJc = 4, AwgJmin = 40, AwgJmax = 70,
                    AwgS1 = 15, AwgS2 = 20, AwgS3 = 18, AwgS4 = 12,
                    AwgH1 = "100-200", AwgH2 = "2", AwgH3 = "3", AwgH4 = "4",
                    AwgHeaderProtectionKey = WgKey, AwgContentPaddingAddition = "10-50"
                }
            }
        };

        foreach (var node in validNodes)
            Assert.IsNull(ServerValidator.Validate(node), $"Expected valid {node.ConfigType} ({node.Address})");
    }

    [TestMethod]
    public void ServerValidatorRejectsInvalidFieldsAcrossProtocolsAndTls()
    {
        static void AssertInvalid(Server server, string expectedFragment = "")
        {
            var error = ServerValidator.Validate(server);
            Assert.IsNotNull(error, $"Expected validation failure for {server.ConfigType}");
            if (expectedFragment.Length > 0)
                StringAssert.Contains(error, expectedFragment);
        }

        AssertInvalid(new SocksServer("", 1080), "address");
        AssertInvalid(new SocksServer("127.0.0.1", 0), "port");
        AssertInvalid(new SocksServer { Address = "127.0.0.1", Port = 65536 }, "port");

        AssertInvalid(new SocksServer("127.0.0.1", 1080) { Username = "only-user" }, "SOCKS");
        AssertInvalid(new SocksServer("127.0.0.1", 1080) { Password = "only-pass" }, "SOCKS");
        AssertInvalid(new HttpServer("127.0.0.1", 8080) { ProtoExtra = { HttpHeaders = "NoColon" } }, "headers");
        AssertInvalid(new HttpServer("127.0.0.1", 8080) { ProtoExtra = { HttpHeaders = "Bad Header: 1" } }, "headers");

        AssertInvalid(new VLESSServer { Address = "127.0.0.1", Port = 443 });
        AssertInvalid(new VLESSServer { Address = "127.0.0.1", Port = 443, Password = Uuid, ProtoExtra = { VlessEncryption = "broken" } }, "VLESS encryption");
        AssertInvalid(new VLESSServer { Address = "127.0.0.1", Port = 443, Password = Uuid, StreamSecurity = "reality" }, "Reality public key");
        AssertInvalid(new VLESSServer { Address = "127.0.0.1", Port = 443, Password = Uuid, StreamSecurity = "reality", PublicKey = "pub", ShortId = "abc" }, "Short ID");
        AssertInvalid(new VLESSServer { Address = "127.0.0.1", Port = 443, Password = Uuid, StreamSecurity = "reality", PublicKey = "pub", ShortId = "0123456789abcdef00" }, "Short ID");
        AssertInvalid(new VLESSServer { Address = "127.0.0.1", Port = 443, Password = Uuid, StreamSecurity = "reality", PublicKey = "pub", ShortId = "zzzz" }, "Short ID");
        AssertInvalid(new VLESSServer { Address = "127.0.0.1", Port = 443, Password = Uuid, Network = "ws", StreamSecurity = "reality", PublicKey = "pub" }, "REALITY");
        AssertInvalid(new VLESSServer { Address = "127.0.0.1", Port = 443, Password = Uuid, Network = "xhttp", Extra = "[1,2]" }, "XHTTP extra");
        AssertInvalid(new VLESSServer { Address = "127.0.0.1", Port = 443, Password = Uuid, Network = "xhttp", Extra = "{bad" }, "XHTTP extra");
        Assert.IsNull(ServerValidator.Validate(new VLESSServer { Address = "127.0.0.1", Port = 443, Password = Uuid, Network = "h2", StreamSecurity = "reality", PublicKey = "pub" }));

        AssertInvalid(new VMessServer { Address = "127.0.0.1", Port = 443 });
        AssertInvalid(new VMessServer { Address = "127.0.0.1", Port = 443, Password = Uuid, ProtoExtra = { AlterId = -1 } });

        AssertInvalid(new TrojanServer { Address = "127.0.0.1", Port = 443 });
        AssertInvalid(new TrojanServer { Address = "127.0.0.1", Port = 443, Password = "pw", Network = "h2", StreamSecurity = "reality", PublicKey = "pub" }, "REALITY");

        AssertInvalid(new ShadowsocksServer { Address = "127.0.0.1", Port = 443, Password = "pw", ProtoExtra = { SsMethod = "unknown-cipher" } }, "Shadowsocks");
        AssertInvalid(new ShadowsocksServer { Address = "127.0.0.1", Port = 443, Password = "", ProtoExtra = { SsMethod = "aes-128-gcm" } }, "Shadowsocks");
        AssertInvalid(new ShadowsocksServer { Address = "127.0.0.1", Port = 443, Password = WgKey, ProtoExtra = { SsMethod = "2022-blake3-aes-128-gcm" } }, "Shadowsocks");

        AssertInvalid(new ShadowsocksRServer { Address = "127.0.0.1", Port = 443, Password = "" });
        AssertInvalid(new ShadowsocksRServer { Address = "127.0.0.1", Port = 443, Password = "pw", EncryptMethod = "" });

        AssertInvalid(new WireGuardServer { Address = "127.0.0.1", Port = 51820, Password = "bad", ProtoExtra = { WgPublicKey = WgKey, WgInterfaceAddress = "10.0.0.2/32" } }, "keys");
        AssertInvalid(new WireGuardServer { Address = "127.0.0.1", Port = 51820, Password = WgKey, ProtoExtra = { WgPublicKey = "bad", WgInterfaceAddress = "10.0.0.2/32" } }, "keys");
        AssertInvalid(new WireGuardServer { Address = "127.0.0.1", Port = 51820, Password = WgKey, PublicKey = WgKey, ProtoExtra = { WgPresharedKey = "bad", WgInterfaceAddress = "10.0.0.2/32" } }, "keys");
        AssertInvalid(new WireGuardServer { Address = "127.0.0.1", Port = 51820, Password = WgKey, PublicKey = WgKey, ProtoExtra = { WgInterfaceAddress = "not-an-ip" } }, "interface address");
        AssertInvalid(new WireGuardServer { Address = "127.0.0.1", Port = 51820, Password = WgKey, PublicKey = WgKey, ProtoExtra = { WgInterfaceAddress = "10.0.0.2/32", WgReserved = "1,2" } }, "reserved");
        AssertInvalid(new WireGuardServer { Address = "127.0.0.1", Port = 51820, Password = WgKey, PublicKey = WgKey, ProtoExtra = { WgInterfaceAddress = "10.0.0.2/32", WgMtu = 0 } });
        Assert.IsNull(ServerValidator.Validate(new WireGuardServer { Address = "127.0.0.1", Port = 51820, Password = WgKey, PublicKey = WgKey, ProtoExtra = { WgInterfaceAddress = "10.0.0.2/32" } }));

        AssertInvalid(new Hysteria2Server { Address = "127.0.0.1", Port = 443, ProtoExtra = { Obfs = "salamander" } }, "obfuscation");
        AssertInvalid(new Hysteria2Server { Address = "127.0.0.1", Port = 443, ProtoExtra = { Obfs = "gecko" } }, "obfuscation");
        AssertInvalid(new Hysteria2Server { Address = "127.0.0.1", Port = 443, ProtoExtra = { Obfs = "gecko", SalamanderPass = "pw", GeckoMinPacketSize = "0" } }, "Gecko");
        AssertInvalid(new Hysteria2Server { Address = "127.0.0.1", Port = 443, ProtoExtra = { Obfs = "gecko", SalamanderPass = "pw", GeckoMinPacketSize = "500", GeckoMaxPacketSize = "100" } }, "exceed");
        AssertInvalid(new Hysteria2Server { Address = "127.0.0.1", Port = 443, ProtoExtra = { Ports = "500-400" } }, "Port hopping");
        AssertInvalid(new Hysteria2Server { Address = "127.0.0.1", Port = 443, ProtoExtra = { Ports = "443,5000-6000", HopInterval = "4" } }, "Hop interval");
        AssertInvalid(new Hysteria2Server { Address = "127.0.0.1", Port = 443, ProtoExtra = { DownMbps = -1 } });

        AssertInvalid(new TUICServer { Address = "127.0.0.1", Port = 443, Username = Guid.Empty.ToString(), Password = "pw" }, "TUIC");
        AssertInvalid(new TUICServer { Address = "127.0.0.1", Port = 443, Username = Uuid, Password = "" }, "TUIC");

        AssertInvalid(new AnyTLSServer { Address = "127.0.0.1", Port = 443, Password = "" });
        AssertInvalid(new AnyTLSServer { Address = "127.0.0.1", Port = 443, Password = "pw", ProtoExtra = { AnyTlsMinIdleSession = -1 } });
        AssertInvalid(new AnyTLSServer { Address = "127.0.0.1", Port = 443, Password = "pw", ProtoExtra = { AnyTlsIdleSessionTimeoutSeconds = -1 } });

        AssertInvalid(new NaiveServer { Address = "127.0.0.1", Port = 443, Password = "" });
        AssertInvalid(new NaiveServer { Address = "127.0.0.1", Port = 443, Password = "pw", ProtoExtra = { InsecureConcurrency = -1 } });

        AssertInvalid(new MASQUEServer { Address = "127.0.0.1", Port = 443, ProtoExtra = { MasquePath = "no-leading-slash" } }, "URI template");
        AssertInvalid(new MASQUEServer { Address = "127.0.0.1", Port = 443, ProtoExtra = { MasqueHeaders = "Invalid" } }, "headers");

        AssertInvalid(new Hysteria2Server { Address = "127.0.0.1", Port = 443, ProtoExtra = { HopIntervalMax = "30" } }, "port hopping");
        AssertInvalid(new Hysteria2Server { Address = "127.0.0.1", Port = 443, ProtoExtra = { Ports = "443,5000-6000", HopInterval = "30", HopIntervalMax = "15" } }, "smaller");
        AssertInvalid(new Hysteria2Server { Address = "127.0.0.1", Port = 443, ProtoExtra = { BbrProfile = "unknown" } }, "BBR profile");

        AssertInvalid(new ShadowTLSServer { Address = "127.0.0.1", Port = 443, Password = "pw", ProtoExtra = { SsMethod = "aes-128-gcm", ShadowTlsVersion = 3, ShadowTlsPassword = "" } });
        AssertInvalid(new ShadowTLSServer { Address = "127.0.0.1", Port = 443, Password = "pw", StreamSecurity = "reality", ProtoExtra = { SsMethod = "aes-128-gcm", ShadowTlsVersion = 3, ShadowTlsPassword = "hpw" } }, "REALITY");
        Assert.IsNull(ServerValidator.Validate(new ShadowTLSServer { Address = "127.0.0.1", Port = 443, Password = "pw", ProtoExtra = { SsMethod = "aes-128-gcm", ShadowTlsVersion = 1 } }));

        AssertInvalid(new SSHServer { Address = "127.0.0.1", Port = 22, Username = "", Password = "pw" }, "SSH");
        AssertInvalid(new SSHServer { Address = "127.0.0.1", Port = 22, Username = "root", Password = "" }, "SSH");
        AssertInvalid(new SSHServer { Address = "127.0.0.1", Port = 22, Username = "root", ProtoExtra = { SshPrivateKey = "/root/.ssh/id_rsa" } }, "inline PEM");
        Assert.IsNull(ServerValidator.Validate(new SSHServer { Address = "127.0.0.1", Port = 22, Username = "root", Password = "pw" }));

        AssertInvalid(new TrojanServer
        {
            Address = "127.0.0.1", Port = 443, Password = "pw", StreamSecurity = "tls",
            Cert = "-----BEGIN CERTIFICATE-----\nnot-base64\n-----END CERTIFICATE-----"
        }, "PEM certificate");

        Assert.IsTrue(ServerValidator.TryNormalizeWireGuardReserved(" 1 , 2 , 3 ", out var csvReserved));
        Assert.AreEqual("1,2,3", csvReserved);
        Assert.IsTrue(ServerValidator.TryNormalizeWireGuardReserved(Convert.ToBase64String(new byte[] { 10, 20, 30 }), out var b64Reserved));
        Assert.AreEqual("10,20,30", b64Reserved);
        Assert.IsTrue(ServerValidator.TryNormalizeWireGuardReserved("", out var emptyReserved));
        Assert.AreEqual("", emptyReserved);
        Assert.IsFalse(ServerValidator.TryNormalizeWireGuardReserved("1,2,256", out _));
        Assert.IsFalse(ServerValidator.TryNormalizeWireGuardReserved(Convert.ToBase64String(new byte[] { 1, 2, 3, 4 }), out _));

        // TrustTunnel validation
        AssertInvalid(new TrustTunnelServer { Address = "127.0.0.1", Port = 443, StreamSecurity = "reality" }, "REALITY");
        AssertInvalid(new TrustTunnelServer { Address = "127.0.0.1", Port = 443, StreamSecurity = "xtls" });
        AssertInvalid(new TrustTunnelServer { Address = "127.0.0.1", Port = 443, EchForceQuery = "full" }, "custom ECH configuration");
        AssertInvalid(new TrustTunnelServer { Address = "127.0.0.1", Port = 443, ProtoExtra = { TrustTunnelMaxConnections = 8, TrustTunnelMaxStreams = 16 } }, "cannot both be set");
        AssertInvalid(new TrustTunnelServer { Address = "127.0.0.1", Port = 443, EchConfigList = "bad!!!" }, "Base64");

        // Mieru validation
        AssertInvalid(new MieruServer { Address = "127.0.0.1", Port = 2012, Username = "", Password = "p", ProtoExtra = { MieruTransport = "TCP" } });
        AssertInvalid(new MieruServer { Address = "127.0.0.1", Port = 2012, Username = "u", Password = "p", ProtoExtra = { MieruTransport = "QUIC" } }, "TCP or UDP");
        AssertInvalid(new MieruServer { Address = "127.0.0.1", Port = 2010, Username = "u", Password = "p", ProtoExtra = { MieruTransport = "UDP", MieruPortRange = "2010-2020,2030-2040" } }, "single start-end");
        AssertInvalid(new MieruServer { Address = "127.0.0.1", Port = 0, Username = "u", Password = "p", ProtoExtra = { MieruTransport = "UDP", MieruPortRange = "2010-2020,3000-4000" } }, "single start-end");
        AssertInvalid(new MieruServer { Address = "127.0.0.1", Port = 2012, Username = "u", Password = "p", ProtoExtra = { MieruTransport = "TCP", MieruTrafficPattern = "not-b64!!!" } }, "Base64");

        // ShadowQUIC validation
        AssertInvalid(new ShadowQUICServer { Address = "127.0.0.1", Port = 443, Username = "u", Password = "" });
        AssertInvalid(new ShadowQUICServer { Address = "127.0.0.1", Port = 443, Username = "u", Password = "p", ProtoExtra = { ShadowQuicVersions = "v3" } }, "v1 or v2");
        AssertInvalid(new ShadowQUICServer { Address = "127.0.0.1", Port = 443, Username = "u", Password = "p", ProtoExtra = { CongestionControl = "unknown" } });

        // AmneziaWG validation
        AssertInvalid(new AmneziaWGServer { Address = "127.0.0.1", Port = 51820, Password = WgKey, ProtoExtra = { WgPublicKey = WgKey, WgInterfaceAddress = "10.0.0.2/32,10.0.0.3/32" } }, "at most one IPv4");
        AssertInvalid(new AmneziaWGServer { Address = "127.0.0.1", Port = 51820, Password = WgKey, ProtoExtra = { WgPublicKey = WgKey, WgInterfaceAddress = "10.0.0.2/32", AwgJmin = 100, AwgJmax = 50 } }, "Jmin cannot exceed Jmax");
        AssertInvalid(new AmneziaWGServer { Address = "127.0.0.1", Port = 51820, Password = WgKey, ProtoExtra = { WgPublicKey = WgKey, WgInterfaceAddress = "10.0.0.2/32", AwgH1 = "200-100" } }, "H1-H4");
        AssertInvalid(new AmneziaWGServer { Address = "127.0.0.1", Port = 51820, Password = WgKey, ProtoExtra = { WgPublicKey = WgKey, WgInterfaceAddress = "10.0.0.2/32", AwgHeaderProtectionKey = "bad-key" } }, "WireGuard keys");
        AssertInvalid(new AmneziaWGServer { Address = "127.0.0.1", Port = 51820, Password = WgKey, ProtoExtra = { WgPublicKey = WgKey, WgInterfaceAddress = "10.0.0.2/32", AwgContentPaddingAddition = "50-10" } }, "content padding");
    }

    [TestMethod]
    public void CoreSelectorRoutesEveryProtocolAndCapabilityToExpectedCore()
    {
        var pem = SelfSignedPem.Value;

        Assert.AreEqual("mihomo", SelectCore(new VLESSServer { Address = "h", Port = 443, Password = Uuid }));
        Assert.AreEqual("mihomo", SelectCore(new VMessServer { Address = "h", Port = 443, Password = Uuid }));
        Assert.AreEqual("mihomo", SelectCore(new TrojanServer { Address = "h", Port = 443, Password = "p", StreamSecurity = "tls" }));
        Assert.AreEqual("mihomo", SelectCore(new ShadowsocksServer { Address = "h", Port = 443, Password = "p", ProtoExtra = { SsMethod = "aes-128-gcm" } }));
        Assert.AreEqual("mihomo", SelectCore(new ShadowsocksRServer { Address = "h", Port = 443, Password = "p" }));
        Assert.AreEqual("mihomo", SelectCore(new SocksServer("h", 1080)));
        Assert.AreEqual("mihomo", SelectCore(new HttpServer("h", 8080)));
        Assert.AreEqual("mihomo", SelectCore(new Hysteria2Server { Address = "h", Port = 443, Password = "p", ProtoExtra = { Ports = "443,5000-6000", HopInterval = "10" } }));
        Assert.AreEqual("mihomo", SelectCore(new TUICServer { Address = "h", Port = 443, Username = Uuid, Password = "p" }));
        Assert.AreEqual("mihomo", SelectCore(new AnyTLSServer { Address = "h", Port = 443, Password = "p" }));
        Assert.AreEqual("mihomo", SelectCore(new WireGuardServer { Address = "h", Port = 51820, Password = WgKey, ProtoExtra = { WgPublicKey = WgKey, WgInterfaceAddress = "10.0.0.2/32,fd00::2/128" } }));

        Assert.AreEqual("mihomo", SelectCore(new VLESSServer
        {
            Address = "h", Port = 443, Password = Uuid, Network = "xhttp", HeaderType = "packet-up",
            StreamSecurity = "tls", Alpn = "h2,http/1.1", Extra = "{}"
        }));

        Assert.AreEqual("xray", SelectCore(new VLESSServer { Address = "h", Port = 443, Password = Uuid, Network = "xhttp", StreamSecurity = "tls", Extra = """{"xPaddingBytes":"100"}""" }));
        Assert.AreEqual("xray", SelectCore(new VLESSServer { Address = "h", Port = 443, Password = Uuid, Network = "xhttp", StreamSecurity = "reality", PublicKey = "pub" }));
        Assert.AreEqual("xray", SelectCore(new VLESSServer { Address = "h", Port = 443, Password = Uuid, Network = "xhttp", StreamSecurity = "tls", EchConfigList = "ech" }));
        Assert.AreEqual("xray", SelectCore(new VLESSServer { Address = "h", Port = 443, Password = Uuid, Network = "xhttp", StreamSecurity = "tls", EchForceQuery = "full" }));
        Assert.AreEqual("xray", SelectCore(new VLESSServer { Address = "h", Port = 443, Password = Uuid, Network = "xhttp", StreamSecurity = "tls", Cert = pem }));
        Assert.AreEqual("xray", SelectCore(new VLESSServer { Address = "h", Port = 443, Password = Uuid, Network = "xhttp", StreamSecurity = "tls", CertSha = "deadbeef" }));
        Assert.AreEqual("xray", SelectCore(new VLESSServer { Address = "h", Port = 443, Password = Uuid, Network = "xhttp", StreamSecurity = "tls", ProtoExtra = { Flow = "xtls-rprx-vision" } }));
        Assert.AreEqual("xray", SelectCore(new VLESSServer { Address = "h", Port = 443, Password = Uuid, Network = "xhttp", StreamSecurity = "tls", ProtoExtra = { VlessEncryption = "mlkem768x25519plus.native.0rtt.s" } }));
        Assert.AreEqual("xray", SelectCore(new VLESSServer { Address = "h", Port = 443, Password = Uuid, Network = "xhttp", StreamSecurity = "tls", Alpn = "h3" }));
        Assert.AreEqual("xray", SelectCore(new VLESSServer { Address = "h", Port = 443, Password = Uuid, Network = "xhttp", StreamSecurity = "tls", MuxEnabled = true }));
        Assert.AreEqual("xray", SelectCore(new VMessServer { Address = "h", Port = 443, Password = Uuid, Network = "xhttp" }));
        Assert.AreEqual("xray", SelectCore(new TrojanServer { Address = "h", Port = 443, Password = "p", Network = "xhttp", StreamSecurity = "tls" }));
        Assert.AreEqual("xray", SelectCore(new ShadowsocksServer { Address = "h", Port = 443, Password = "p", Network = "xhttp" }));

        Assert.AreEqual("sing-box", SelectCore(new NaiveServer { Address = "h", Port = 443, Username = "u", Password = "p" }));
        Assert.AreEqual("sing-box", SelectCore(new MASQUEServer { Address = "h", Port = 443 }));
        Assert.AreEqual("sing-box", SelectCore(new VLESSServer { Address = "h", Port = 443, Password = Uuid, StreamSecurity = "tls", Cert = pem }));
        Assert.AreEqual("sing-box", SelectCore(new SocksServer("h", 1080) { ProtoExtra = { Uot = true } }));
        Assert.AreEqual("sing-box", SelectCore(new VMessServer { Address = "h", Port = 443, Password = Uuid, Network = "quic" }));
        Assert.AreEqual("sing-box", SelectCore(new TrojanServer { Address = "h", Port = 443, Password = "p", Network = "h2", StreamSecurity = "tls" }));
        Assert.AreEqual("sing-box", SelectCore(new TrojanServer { Address = "h", Port = 443, Password = "p", StreamSecurity = "none" }));
        Assert.AreEqual("sing-box", SelectCore(new VMessServer { Address = "h", Port = 443, Password = Uuid, Network = "h2", StreamSecurity = "none" }));
        Assert.AreEqual("sing-box", SelectCore(new HttpServer("h", 443) { StreamSecurity = "tls", ProtoExtra = { HttpHeaders = "Cookie: a=1\nCookie: b=2" } }));
        Assert.AreEqual("sing-box", SelectCore(new HttpServer("h", 443) { StreamSecurity = "tls", Alpn = "h2" }));
        Assert.AreEqual("sing-box", SelectCore(new HttpServer("h", 443) { StreamSecurity = "tls", Fingerprint = "chrome" }));
        Assert.AreEqual("sing-box", SelectCore(new Hysteria2Server { Address = "h", Port = 443, Password = "p", ProtoExtra = { Ports = "443,5000-6000", HopInterval = "3s" } }));
        Assert.AreEqual("sing-box", SelectCore(new Hysteria2Server { Address = "h", Port = 443, Password = "p", ProtoExtra = { Ports = "443,5000-6000", HopIntervalMax = "30" } }));
        Assert.AreEqual("sing-box", SelectCore(new Hysteria2Server { Address = "h", Port = 443, Password = "p", ProtoExtra = { BbrProfile = "aggressive" } }));
        Assert.AreEqual("mihomo", SelectCore(new ShadowTLSServer { Address = "h", Port = 443, Password = "p", ProtoExtra = { ShadowTlsVersion = 3, ShadowTlsPassword = "hpw" } }));
        Assert.AreEqual("sing-box", SelectCore(new ShadowTLSServer { Address = "h", Port = 443, Password = "p", ProtoExtra = { ShadowTlsVersion = 1 } }));
        Assert.AreEqual("sing-box", SelectCore(new ShadowTLSServer { Address = "h", Port = 443, Password = "p", Cert = pem, ProtoExtra = { ShadowTlsVersion = 3, ShadowTlsPassword = "hpw" } }));
        Assert.AreEqual("mihomo", SelectCore(new SSHServer { Address = "h", Port = 22, Username = "root", Password = "p" }));
        Assert.AreEqual("sing-box", SelectCore(new SSHServer { Address = "h", Port = 22, Username = "root", Password = "p", ProtoExtra = { SshClientVersion = "SSH-2.0-OpenSSH_9.6" } }));
        Assert.AreEqual("sing-box", SelectCore(new WireGuardServer { Address = "h", Port = 51820, Password = WgKey, ProtoExtra = { WgPublicKey = WgKey, WgInterfaceAddress = "10.0.0.2/32,10.0.0.3/32" } }));
        Assert.AreEqual("sing-box", SelectCore(new ShadowsocksServer { Address = "h", Port = 443, Password = "p", ProtoExtra = { SsPlugin = "v2ray-plugin", SsPluginOptions = "mode=quic" } }));

        Assert.AreEqual("mihomo", SelectCore(new TrustTunnelServer { Address = "h", Port = 443, Cert = pem }));
        Assert.AreEqual("mihomo", SelectCore(new MieruServer { Address = "h", Port = 2012, Username = "u", Password = "p", ProtoExtra = { MieruTransport = "UDP" } }));
        Assert.AreEqual("mihomo", SelectCore(new ShadowQUICServer { Address = "h", Port = 443, Username = "u", Password = "p" }));
        Assert.AreEqual("mihomo", SelectCore(new AmneziaWGServer { Address = "h", Port = 51820, Password = WgKey, ProtoExtra = { WgPublicKey = WgKey, WgInterfaceAddress = "10.0.0.2/32" } }));

        Assert.IsTrue(new TrustTunnelServer { Address = "h", Port = 443 }.SupportsTcpPreScreen());
        Assert.IsFalse(new TrustTunnelServer { Address = "h", Port = 443, ProtoExtra = { TrustTunnelQuic = true } }.SupportsTcpPreScreen());
        Assert.IsTrue(new MieruServer { Address = "h", Port = 2012, ProtoExtra = { MieruTransport = "TCP" } }.SupportsTcpPreScreen());
        Assert.IsFalse(new MieruServer { Address = "h", Port = 2012, ProtoExtra = { MieruTransport = "UDP" } }.SupportsTcpPreScreen());
        Assert.IsFalse(new ShadowQUICServer { Address = "h", Port = 443 }.SupportsTcpPreScreen());
        Assert.IsFalse(new AmneziaWGServer { Address = "h", Port = 51820 }.SupportsTcpPreScreen());

        var tunMode = new Netch.Models.Modes.TunMode.TunMode();
        Netch.Controllers.MainController.EnsureUdpCompatibility(new TrustTunnelServer { Address = "h", Port = 443 }, tunMode);
        Netch.Controllers.MainController.EnsureUdpCompatibility(new MieruServer { Address = "h", Port = 2012 }, tunMode);
        Netch.Controllers.MainController.EnsureUdpCompatibility(new ShadowQUICServer { Address = "h", Port = 443 }, tunMode);
        Netch.Controllers.MainController.EnsureUdpCompatibility(new AmneziaWGServer { Address = "h", Port = 51820 }, tunMode);
    }

    [TestMethod]
    public void MihomoSingboxAndXrayConfigBuildersEmitProtocolParameters()
    {
        var pem = SelfSignedPem.Value;

        var mihomoWsNode = new VLESSServer
        {
            Address = "ws.example", Port = 443, Password = Uuid,
            Network = "ws", RequestHost = "cdn.example", Path = "/ws?ed=2048",
            StreamSecurity = "reality", PublicKey = "pub", ShortId = "ab", MuxEnabled = false
        };
        var mihomoWs = BuildMihomoJson(mihomoWsNode);
        var wsOpts = mihomoWs.RootElement.GetProperty("proxies")[0];
        Assert.AreEqual("firefox", wsOpts.GetProperty("client-fingerprint").GetString());
        Netch.Global.Settings.V2RayConfig.CoreBasicItem.DefFingerprint = "";
        Assert.AreEqual("chrome", BuildMihomoJson(mihomoWsNode).RootElement.GetProperty("proxies")[0].GetProperty("client-fingerprint").GetString());
        Assert.AreEqual("/ws", wsOpts.GetProperty("ws-opts").GetProperty("path").GetString());
        Assert.AreEqual(2048, wsOpts.GetProperty("ws-opts").GetProperty("max-early-data").GetInt32());
        Assert.AreEqual("Sec-WebSocket-Protocol", wsOpts.GetProperty("ws-opts").GetProperty("early-data-header-name").GetString());

        var mihomoUpgrade = BuildMihomoJson(new VMessServer
        {
            Address = "up.example", Port = 443, Password = Uuid,
            Network = "httpupgrade", StreamSecurity = "tls", Sni = "sni.example", MuxEnabled = false
        });
        var upProxy = mihomoUpgrade.RootElement.GetProperty("proxies")[0];
        Assert.IsTrue(upProxy.GetProperty("ws-opts").GetProperty("v2ray-http-upgrade").GetBoolean());
        Assert.AreEqual("sni.example", upProxy.GetProperty("ws-opts").GetProperty("headers").GetProperty("Host").GetString());

        var mihomoSs = BuildMihomoJson(new ShadowsocksServer
        {
            Address = "ss.example", Port = 443, Password = "pw",
            StreamSecurity = "tls", AllowInsecure = true,
            ProtoExtra = { SsMethod = "chacha20-poly1305", Uot = true, SsPlugin = "v2ray-plugin", SsPluginOptions = "mode=websocket;mux=1;tls" }
        });
        var ssProxy = mihomoSs.RootElement.GetProperty("proxies")[0];
        Assert.AreEqual("chacha20-ietf-poly1305", ssProxy.GetProperty("cipher").GetString());
        Assert.IsTrue(ssProxy.GetProperty("udp-over-tcp").GetBoolean());
        Assert.AreEqual(2, ssProxy.GetProperty("udp-over-tcp-version").GetInt32());
        Assert.IsTrue(ssProxy.GetProperty("plugin-opts").GetProperty("skip-cert-verify").GetBoolean());
        Assert.IsTrue(ssProxy.GetProperty("plugin-opts").GetProperty("mux").GetBoolean());

        Assert.ThrowsExactly<MessageException>(() => InvokeMihomoBuild(new TrojanServer { Address = "h", Port = 443, Password = "p", StreamSecurity = "tls", CertSha = "abc" }));
        Assert.ThrowsExactly<MessageException>(() => InvokeMihomoBuild(new VLESSServer { Address = "h", Port = 443, Password = Uuid, StreamSecurity = "tls", EchConfigList = "ech", MuxEnabled = false }));
        Assert.ThrowsExactly<MessageException>(() => InvokeMihomoBuild(new TrojanServer { Address = "h", Port = 443, Password = "p", StreamSecurity = "tls", ProtoExtra = { Flow = "xtls-rprx-vision" } }));
        Assert.ThrowsExactly<MessageException>(() => InvokeMihomoBuild(new VMessServer { Address = "h", Port = 443, Password = Uuid, MuxEnabled = true }));

        static JsonElement FirstMihomoProxy(Server server) =>
            BuildMihomoJson(server).RootElement.EnumerateObject().First(p => p.Name.Equals("proxies", StringComparison.OrdinalIgnoreCase)).Value[0];

        var ttBlankCert = FirstMihomoProxy(new TrustTunnelServer { Address = "h", Port = 443, Cert = "   ", CertSha = "   ", Sni = "   ", Fingerprint = "   ", EchForceQuery = "NONE", ProtoExtra = { CongestionControl = "   ", BbrProfile = "   ", TrustTunnelNameCertVerify = "   " } });
        Assert.IsNull(ServerValidator.Validate(new TrustTunnelServer { Address = "h", Port = 443, Cert = "   ", CertSha = "   ", Sni = "   ", Fingerprint = "   ", EchForceQuery = "NONE" }));
        Assert.AreEqual("h", ttBlankCert.GetProperty("sni").GetString());
        Assert.IsFalse(ttBlankCert.TryGetProperty("client-fingerprint", out _));
        Assert.IsFalse(ttBlankCert.TryGetProperty("certificate", out _));
        Assert.IsFalse(ttBlankCert.TryGetProperty("name-cert-verify", out _));
        Assert.IsFalse(ttBlankCert.TryGetProperty("congestion-controller", out _));
        Assert.IsFalse(ttBlankCert.TryGetProperty("bbr-profile", out _));

        var ttBlankEch = new TrustTunnelServer { Address = "h", Port = 443, EchForceQuery = "   ", Fingerprint = "NONE" };
        Assert.IsNull(ServerValidator.Validate(ttBlankEch));
        Assert.IsFalse(FirstMihomoProxy(ttBlankEch).TryGetProperty("client-fingerprint", out _));

        var mieruUdpTrim = new MieruServer { Address = "h", Port = 2012, Username = "u", Password = "p", ProtoExtra = { MieruTransport = " UDP ", MieruPortRange = "   ", MieruMultiplexing = "   ", MieruHandshakeMode = "   ", MieruTrafficPattern = "   " } };
        Assert.IsFalse(mieruUdpTrim.SupportsTcpPreScreen());
        var mieruProxy = FirstMihomoProxy(mieruUdpTrim);
        Assert.AreEqual(2012, mieruProxy.GetProperty("port").GetInt32());
        Assert.IsFalse(mieruProxy.TryGetProperty("port-range", out _));
        Assert.IsFalse(mieruProxy.TryGetProperty("multiplexing", out _));
        Assert.IsFalse(mieruProxy.TryGetProperty("handshake-mode", out _));
        Assert.IsFalse(mieruProxy.TryGetProperty("traffic-pattern", out _));

        var sqBlank = FirstMihomoProxy(new ShadowQUICServer { Address = "h", Port = 443, Username = "u", Password = "p", Sni = "   ", ProtoExtra = { CongestionControl = "   ", BbrProfile = "   " } });
        Assert.IsFalse(sqBlank.TryGetProperty("sni", out _));
        Assert.IsFalse(sqBlank.TryGetProperty("congestion-controller", out _));
        Assert.IsFalse(sqBlank.TryGetProperty("bbr-profile", out _));

        var awgOnlyWhitespace = FirstMihomoProxy(new AmneziaWGServer { Address = "h", Port = 51820, Password = WgKey, ProtoExtra = { WgPublicKey = WgKey, WgInterfaceAddress = "10.0.0.2/32", AwgH1 = "   ", AwgRekeyAfterTime = "   " } });
        Assert.IsFalse(awgOnlyWhitespace.TryGetProperty("amnezia-wg-option", out _));

        var awgBlank = FirstMihomoProxy(new AmneziaWGServer { Address = "h", Port = 51820, Password = WgKey, ProtoExtra = { WgPublicKey = WgKey, WgInterfaceAddress = "10.0.0.2/32", AwgJc = 4, AwgH1 = "   ", AwgRekeyAfterTime = "   " } })
            .GetProperty("amnezia-wg-option");
        Assert.AreEqual(4, awgBlank.GetProperty("jc").GetInt32());
        Assert.IsFalse(awgBlank.TryGetProperty("h1", out _));
        Assert.IsFalse(awgBlank.TryGetProperty("rekey-after-time", out _));

        var singAnyTls = BuildSingboxJson(new AnyTLSServer
        {
            Address = "any.example", Port = 443, Password = "pw", Fingerprint = "firefox",
            ProtoExtra = { AnyTlsIdleSessionCheckIntervalSeconds = 20, AnyTlsIdleSessionTimeoutSeconds = 45, AnyTlsMinIdleSession = 2 }
        });
        var anyOut = singAnyTls.RootElement.GetProperty("outbounds")[0];
        Assert.AreEqual("20s", anyOut.GetProperty("idle_session_check_interval").GetString());
        Assert.AreEqual("45s", anyOut.GetProperty("idle_session_timeout").GetString());
        Assert.AreEqual(2, anyOut.GetProperty("min_idle_session").GetInt32());
        Assert.AreEqual("firefox", anyOut.GetProperty("tls").GetProperty("utls").GetProperty("fingerprint").GetString());

        var singHy2 = BuildSingboxJson(new Hysteria2Server
        {
            Address = "hy2.example", Port = 443, Password = "pw", Cert = pem,
            ProtoExtra = { Ports = "443,5000-6000", HopInterval = "15" }
        });
        var hy2Out = singHy2.RootElement.GetProperty("outbounds")[0];
        Assert.IsFalse(hy2Out.TryGetProperty("server_port", out _));
        CollectionAssert.AreEqual(new[] { "443:443", "5000:6000" },
            hy2Out.GetProperty("server_ports").EnumerateArray().Select(p => p.GetString()).ToArray());
        Assert.AreEqual("15s", hy2Out.GetProperty("hop_interval").GetString());
        Assert.IsFalse(hy2Out.GetProperty("tls").GetProperty("insecure").GetBoolean());
        Assert.IsTrue(hy2Out.GetProperty("tls").GetProperty("certificate").GetArrayLength() > 0);

        var singWg = BuildSingboxJson(new WireGuardServer
        {
            Address = "wg.example", Port = 51820, Password = WgKey, PublicKey = WgKey,
            ProtoExtra = { WgInterfaceAddress = "10.0.0.2/32", WgReserved = "1,2,3" }
        });
        Assert.IsFalse(singWg.RootElement.GetProperty("route").GetProperty("auto_detect_interface").GetBoolean());
        Assert.AreEqual(WgKey, singWg.RootElement.GetProperty("endpoints")[0].GetProperty("peers")[0].GetProperty("public_key").GetString());

        var singSocks = BuildSingboxJson(new SocksServer("socks.example", 1080) { ProtoExtra = { Uot = true } });
        Assert.IsTrue(singSocks.RootElement.GetProperty("outbounds")[0].GetProperty("udp_over_tcp").GetBoolean());
        Assert.IsFalse(singSocks.RootElement.GetProperty("outbounds")[0].TryGetProperty("tls", out _));

        var singHttp = BuildSingboxJson(new HttpServer("http.example", 443)
        {
            StreamSecurity = "tls", Alpn = "h2", Fingerprint = "chrome",
            ProtoExtra = { HttpHeaders = "Cookie: a=1\nCookie: b=2" }
        });
        var cookieValues = singHttp.RootElement.GetProperty("outbounds")[0].GetProperty("headers").GetProperty("Cookie")
            .EnumerateArray().Select(v => v.GetString()).ToArray();
        CollectionAssert.AreEqual(new[] { "a=1", "b=2" }, cookieValues);

        Assert.ThrowsExactly<MessageException>(() => InvokeSingboxBuild(new VLESSServer { Address = "h", Port = 443, Password = Uuid, StreamSecurity = "tls", CertSha = "abc", MuxEnabled = false }));
        Assert.ThrowsExactly<MessageException>(() => InvokeSingboxBuild(new VLESSServer { Address = "h", Port = 443, Password = Uuid, StreamSecurity = "tls", EchConfigList = "ech", MuxEnabled = false }));
        Assert.ThrowsExactly<MessageException>(() => InvokeSingboxBuild(new VLESSServer { Address = "h", Port = 443, Password = Uuid, StreamSecurity = "reality", PublicKey = "pub", SpiderX = "/custom", MuxEnabled = false }));

        var xrayReality = V2rayConfigUtils.GenerateClientConfig(new VLESSServer
        {
            Address = "xr.example", Port = 443, Password = Uuid,
            Network = "xhttp", HeaderType = "packet-up", Extra = """{"xPaddingBytes":"100"}""",
            StreamSecurity = "reality", PublicKey = "pub", ShortId = "ab", SpiderX = "/sp", Mldsa65Verify = "pq",
            ProtoExtra = { Flow = "xtls-rprx-vision", VlessEncryption = "mlkem768x25519plus.native.0rtt.s" }
        }, "127.0.0.1", 19081);
        var xrUser = xrayReality.outbounds[0].settings.vnext[0].users[0];
        Assert.AreEqual("mlkem768x25519plus.native.0rtt.s", xrUser.encryption);
        Assert.AreEqual("xtls-rprx-vision", xrUser.flow);
        Assert.AreEqual("/sp", xrayReality.outbounds[0].streamSettings.realitySettings.spiderX);
        Assert.AreEqual("pq", xrayReality.outbounds[0].streamSettings.realitySettings.mldsa65Verify);

        var xrayTlsCert = V2rayConfigUtils.GenerateClientConfig(new VLESSServer
        {
            Address = "tls.example", Port = 443, Password = Uuid,
            Network = "h2", RequestHost = "h1.example,h2.example", Path = "/h2",
            StreamSecurity = "tls", Cert = pem, EchConfigList = "ech1", EchForceQuery = "full"
        }, "127.0.0.1", 19081);
        var xrTls = xrayTlsCert.outbounds[0].streamSettings.tlsSettings;
        Assert.AreEqual(true, xrTls.disableSystemRoot);
        Assert.AreEqual(false, xrTls.allowInsecure);
        Assert.AreEqual("ech1", xrTls.echConfigList);
        Assert.AreEqual("full", xrTls.echForceQuery);
        CollectionAssert.AreEqual(new[] { "h1.example", "h2.example" }, xrayTlsCert.outbounds[0].streamSettings.httpSettings.host);

        var xrayTcp = V2rayConfigUtils.GenerateClientConfig(new VMessServer
        {
            Address = "tcp.example", Port = 80, Password = Uuid,
            Network = "tcp", HeaderType = "http", RequestHost = "cam.example", Path = "/fake",
            MuxEnabled = false
        }, "127.0.0.1", 19081);
        var tcpReq = JsonSerializer.Serialize(xrayTcp.outbounds[0].streamSettings.tcpSettings.header.request);
        StringAssert.Contains(tcpReq, "cam.example");
        StringAssert.Contains(tcpReq, "/fake");

        var xrayKcpDns = V2rayConfigUtils.GenerateClientConfig(new VMessServer
        {
            Address = "kcp.example", Port = 443, Password = Uuid,
            Network = "kcp", HeaderType = "dns", RequestHost = "dns.example", Path = "my-seed",
            MuxEnabled = false
        }, "127.0.0.1", 19081);
        var udpMasks = xrayKcpDns.outbounds[0].streamSettings.finalmask.udp;
        Assert.AreEqual("dns.example", udpMasks[0].settings.domain);
        Assert.AreEqual("mkcp-aes128gcm", udpMasks[1].type);

        var xrayWg = V2rayConfigUtils.GenerateClientConfig(new WireGuardServer
        {
            Address = "wg.example", Port = 51820, Password = WgKey, PublicKey = WgKey,
            ProtoExtra = { WgInterfaceAddress = "10.0.0.2/32" }
        }, "127.0.0.1", 19081);
        Assert.AreEqual(WgKey, xrayWg.outbounds[0].settings.peers[0].publicKey);

        var xrayHy2 = V2rayConfigUtils.GenerateClientConfig(new Hysteria2Server
        {
            Address = "hy2.example", Port = 443, Password = "pw",
            ProtoExtra = { UpMbps = 25, DownMbps = 80, Ports = "443,5000-6000", HopInterval = "12", SalamanderPass = "sal" }
        }, "127.0.0.1", 19081);
        Assert.AreEqual("25mbps", xrayHy2.outbounds[0].streamSettings.hysteriaSettings.up);
        Assert.AreEqual("80mbps", xrayHy2.outbounds[0].streamSettings.hysteriaSettings.down);
        Assert.AreEqual("salamander", xrayHy2.outbounds[0].streamSettings.finalmask.udp[0].type);
    }

    [TestMethod]
    public void GeneratedConfigsPassCoreSchemaChecksWhenBinariesExist()
    {
        var repoRoot = FindRepoRoot();
        if (repoRoot == null)
            return;

        var pem = SelfSignedPem.Value;
        var utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        var temp = Directory.CreateTempSubdirectory("netch-core-schema-");
        try
        {
            var (sshPem, sshHostKey) = CreateEphemeralSshCredentials();

            var mihomoExe = Path.Combine(repoRoot, "Other", "release", "mihomo.exe");
            if (File.Exists(mihomoExe))
            {
                var mihomoServers = new Server[]
                {
                    new TUICServer { Address = "127.0.0.1", Port = 443, Username = Uuid, Password = "pw", ProtoExtra = { CongestionControl = "bbr", TuicUdpRelayMode = "quic", TuicReduceRtt = true, TuicDisableSni = true } },
                    new AnyTLSServer { Address = "127.0.0.1", Port = 443, Password = "pw", ProtoExtra = { AnyTlsIdleSessionCheckIntervalSeconds = 20, AnyTlsIdleSessionTimeoutSeconds = 45, AnyTlsMinIdleSession = 2 } },
                    new Hysteria2Server { Address = "127.0.0.1", Port = 443, Password = "pw", ProtoExtra = { Obfs = "gecko", SalamanderPass = "obfs-pw", GeckoMinPacketSize = "128", GeckoMaxPacketSize = "512", Ports = "443,5000-6000", HopInterval = "15" } },
                    new ShadowsocksServer { Address = "127.0.0.1", Port = 443, Password = "pw", ProtoExtra = { SsMethod = "aes-128-gcm", Uot = true, SsPlugin = "v2ray-plugin", SsPluginOptions = "mode=websocket;mux=1;tls" } },
                    new WireGuardServer { Address = "127.0.0.1", Port = 51820, Password = WgKey, ProtoExtra = { WgPublicKey = WgKey, WgInterfaceAddress = "10.0.0.2/32", WgReserved = "1,2,3" } },
                    new VLESSServer { Address = "127.0.0.1", Port = 443, Password = Uuid, Network = "xhttp", HeaderType = "packet-up", StreamSecurity = "tls", MuxEnabled = false },
                    new ShadowTLSServer { Address = "127.0.0.1", Port = 443, Password = "pw", Sni = "stls.example", Alpn = "h2", Fingerprint = "chrome", ProtoExtra = { SsMethod = "aes-128-gcm", ShadowTlsVersion = 3, ShadowTlsPassword = "hpw", Uot = true } },
                    new SSHServer { Address = "127.0.0.1", Port = 22, Username = "root", ProtoExtra = { SshPrivateKey = sshPem, SshHostKey = sshHostKey, SshHostKeyAlgorithms = "ssh-ed25519" } },
                    new TrustTunnelServer { Address = "127.0.0.1", Port = 443, Username = "tt-u", Password = "tt-p", Sni = "tt.local", Alpn = "h2", Fingerprint = "chrome", Cert = pem, EchConfigList = "AQIDBA==", ProtoExtra = { TrustTunnelHealthCheck = true, TrustTunnelNameCertVerify = "tt.local", TrustTunnelMaxConnections = 8, TrustTunnelMinStreams = 4 } },
                    new TrustTunnelServer { Address = "127.0.0.1", Port = 443, Username = "tt-u", Password = "tt-p", Sni = "tt-q.local", ProtoExtra = { TrustTunnelQuic = true, CongestionControl = "bbr", BbrProfile = "aggressive", TrustTunnelMaxStreams = 16 } },
                    new MieruServer { Address = "127.0.0.1", Port = 2012, Username = "m-u", Password = "m-p", ProtoExtra = { MieruTransport = "UDP", MieruMultiplexing = "MULTIPLEXING_LOW", MieruHandshakeMode = "HANDSHAKE_NO_WAIT" } },
                    new MieruServer { Address = "127.0.0.1", Port = 2010, Username = "m-u", Password = "m-p", ProtoExtra = { MieruTransport = "TCP", MieruPortRange = "2010-2020", MieruMultiplexing = "MULTIPLEXING_HIGH", MieruHandshakeMode = "HANDSHAKE_STANDARD", MieruTrafficPattern = "CgA=" } },
                    new ShadowQUICServer { Address = "127.0.0.1", Port = 10443, Username = "sq-u", Password = "sq-p", Sni = "sq.example", Alpn = "h3", ProtoExtra = { ShadowQuicVersions = "v1,v2", ShadowQuicUdpOverStream = true, ShadowQuicZeroRtt = true, ShadowQuicKeepAliveInterval = 10000, CongestionControl = "bbr", BbrProfile = "standard", UpMbps = 100, DownMbps = 200, ShadowQuicCwnd = 32, ShadowQuicMaxDatagramFrameSize = 1400, ShadowQuicMaxOpenStreams = 1024, ShadowQuicDisableMtuDiscovery = true } },
                    new AmneziaWGServer { Address = "127.0.0.1", Port = 51820, Password = WgKey, ProtoExtra = { WgPublicKey = WgKey, WgInterfaceAddress = "10.0.0.2/32", AwgJc = 4, AwgJmin = 40, AwgJmax = 70, AwgS1 = 15, AwgS2 = 30, AwgS3 = 10, AwgS4 = 20, AwgH1 = "100000-200000", AwgH2 = "200001-300000", AwgH3 = "300001-400000", AwgH4 = "400001-500000", AwgI1 = "<b 0xc700000001><r 16>" } },
                    new AmneziaWGServer { Address = "127.0.0.1", Port = 51820, Password = WgKey, ProtoExtra = { WgPublicKey = WgKey, WgInterfaceAddress = "10.0.0.2/32,fd00::2/128", AwgVersion = 3, AwgHeaderProtectionKey = WgKey, AwgContentPaddingAddition = "10-100", AwgJc = 4, AwgJmin = 40, AwgJmax = 70, AwgS1 = 15, AwgS2 = 30, AwgS3 = 20, AwgS4 = 12, AwgH1 = "1", AwgH2 = "2", AwgH3 = "3", AwgH4 = "4", AwgRandomTrailers = true, AwgDisableCookies = true } }
                };
                for (var i = 0; i < mihomoServers.Length; i++)
                {
                    var server = mihomoServers[i];
                    var path = Path.Combine(temp.FullName, $"mihomo-{i}-{server.ConfigType}.json");
                    File.WriteAllText(path, JsonSerializer.Serialize(InvokeMihomoBuild(server), Netch.Global.NewCustomJsonSerializerOptions()), utf8NoBom);
                    RunSchemaCheck(mihomoExe, $"-t -d \"{temp.FullName}\" -f \"{path}\"", $"mihomo {server.ConfigType} #{i}");
                }
            }

            var singboxExe = Path.Combine(repoRoot, "Storage", "sing_box", "sing-box.exe");
            if (File.Exists(singboxExe))
            {
                var singboxServers = new Server[]
                {
                    new TUICServer { Address = "127.0.0.1", Port = 443, Username = Uuid, Password = "pw", Cert = pem, ProtoExtra = { CongestionControl = "bbr", TuicUdpRelayMode = "quic", TuicReduceRtt = true, TuicDisableSni = true } },
                    new AnyTLSServer { Address = "127.0.0.1", Port = 443, Password = "pw", Fingerprint = "chrome", ProtoExtra = { AnyTlsIdleSessionCheckIntervalSeconds = 20, AnyTlsIdleSessionTimeoutSeconds = 45, AnyTlsMinIdleSession = 2 } },
                    new Hysteria2Server { Address = "127.0.0.1", Port = 443, Password = "pw", ProtoExtra = { Obfs = "gecko", SalamanderPass = "obfs-pw", GeckoMinPacketSize = "128", GeckoMaxPacketSize = "512", Ports = "443,5000-6000", HopInterval = "15", HopIntervalMax = "30", BbrProfile = "standard" } },
                    new NaiveServer { Address = "127.0.0.1", Port = 443, Username = "u", Password = "p", ProtoExtra = { NaiveQuic = true, CongestionControl = "bbr2", InsecureConcurrency = 2, Uot = true } },
                    new WireGuardServer { Address = "127.0.0.1", Port = 51820, Password = WgKey, ProtoExtra = { WgPublicKey = WgKey, WgInterfaceAddress = "10.0.0.2/32", WgReserved = "1,2,3" } },
                    new HttpServer("127.0.0.1", 443, "u", "p") { StreamSecurity = "tls", Alpn = "h2", Fingerprint = "chrome", ProtoExtra = { HttpHeaders = "Cookie: a=1\nCookie: b=2" } },
                    new SocksServer("127.0.0.1", 1080) { ProtoExtra = { Uot = true } },
                    new ShadowTLSServer { Address = "127.0.0.1", Port = 443, Password = "pw", Sni = "v1.example", ProtoExtra = { SsMethod = "aes-128-gcm", ShadowTlsVersion = 1 } },
                    new ShadowTLSServer { Address = "127.0.0.1", Port = 444, Password = "pw", Sni = "v3.example", Cert = pem, ProtoExtra = { SsMethod = "aes-128-gcm", ShadowTlsVersion = 3, ShadowTlsPassword = "hpw", Uot = true } },
                    new SSHServer { Address = "127.0.0.1", Port = 22, Username = "root", ProtoExtra = { SshPrivateKey = sshPem, SshHostKey = sshHostKey, SshHostKeyAlgorithms = "ssh-ed25519", SshClientVersion = "SSH-2.0-OpenSSH_9.6" } }
                };
                for (var i = 0; i < singboxServers.Length; i++)
                {
                    var server = singboxServers[i];
                    var path = Path.Combine(temp.FullName, $"singbox-{i}-{server.ConfigType}.json");
                    File.WriteAllText(path, JsonSerializer.Serialize(InvokeSingboxBuild(server), Netch.Global.NewCustomJsonSerializerOptions()), utf8NoBom);
                    RunSchemaCheck(singboxExe, $"check -c \"{path}\"", $"sing-box {server.ConfigType} #{i}");
                }
            }

            var xrayExe = Path.Combine(repoRoot, "Other", "release", "xray.exe");
            if (File.Exists(xrayExe))
            {
                var xrayServers = new Server[]
                {
                    new VLESSServer { Address = "127.0.0.1", Port = 443, Password = Uuid, Network = "xhttp", HeaderType = "packet-up", Extra = """{"xPaddingBytes":"100-200"}""", StreamSecurity = "tls", EchConfigList = "ech-test", EchForceQuery = "full" },
                    new VLESSServer { Address = "127.0.0.1", Port = 443, Password = Uuid, Network = "xhttp", StreamSecurity = "reality", PublicKey = "6AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA", ShortId = "ab", SpiderX = "/sp" }
                };
                for (var i = 0; i < xrayServers.Length; i++)
                {
                    var path = Path.Combine(temp.FullName, $"xray-{i}.json");
                    var cfg = V2rayConfigUtils.GenerateClientConfig(xrayServers[i], "127.0.0.1", 19081);
                    File.WriteAllText(path, JsonSerializer.Serialize(cfg, Netch.Global.NewCustomJsonSerializerOptions()), utf8NoBom);
                    RunSchemaCheck(xrayExe, $"run -test -c \"{path}\"", $"xray #{i}");
                }
            }
        }
        finally
        {
            temp.Delete(recursive: true);
        }
    }

    private static string SelectCore(Server server)
    {
        if (CoreSelector.UsesXhttp(server))
        {
            var canMihomo = (bool)typeof(CoreSelector).GetMethod("CanUseMihomoXhttp", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [server])!;
            return canMihomo ? "mihomo" : "xray";
        }
        var usesSingbox = (bool)typeof(CoreSelector).GetMethod("UsesSingbox", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [server])!;
        return usesSingbox ? "sing-box" : "mihomo";
    }

    private static object InvokeMihomoBuild(Server server)
    {
        try
        {
            var builder = typeof(MihomoController).Assembly.GetType("Netch.Servers.MihomoConfig")!;
            return builder.GetMethod("Build", BindingFlags.Static | BindingFlags.NonPublic)!
                .Invoke(null, [server, "127.0.0.1", "127.0.0.1", (ushort)19081])!;
        }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            throw ex.InnerException;
        }
    }

    private static JsonDocument BuildMihomoJson(Server server) =>
        JsonDocument.Parse(JsonSerializer.Serialize(InvokeMihomoBuild(server), Netch.Global.NewCustomJsonSerializerOptions()));

    private static object InvokeSingboxBuild(Server server)
    {
        try
        {
            var controller = (SingboxController)RuntimeHelpers.GetUninitializedObject(typeof(SingboxController));
            controller.LocalAddress = "127.0.0.1";
            controller.Socks5LocalPort = 19081;
            return typeof(SingboxController).GetMethod("BuildConfig", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(controller, [server])!;
        }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            throw ex.InnerException;
        }
    }

    private static JsonDocument BuildSingboxJson(Server server) =>
        JsonDocument.Parse(JsonSerializer.Serialize(InvokeSingboxBuild(server), Netch.Global.NewCustomJsonSerializerOptions()));

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Netch.sln")))
                return dir.FullName;
            dir = dir.Parent;
        }
        return null;
    }

    private static void RunSchemaCheck(string executable, string arguments, string label)
    {
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = executable,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        })!;
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        Assert.IsTrue(process.WaitForExit(15000), $"{label} timed out");
        Assert.AreEqual(0, process.ExitCode, $"{label} failed ({process.ExitCode}):\n{stdout}\n{stderr}");
    }

    private static string CreateSelfSignedPem()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=netch.test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var cert = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));
        return cert.ExportCertificatePem();
    }

    private static (string Pem, string Ed25519HostKey) CreateEphemeralSshCredentials()
    {
        using var rsa = RSA.Create(2048);
        var pem = rsa.ExportRSAPrivateKeyPem();

        var algo = "ssh-ed25519"u8;
        var blob = new byte[4 + algo.Length + 4 + 32];
        blob[3] = (byte)algo.Length;
        algo.CopyTo(blob.AsSpan(4));
        blob[4 + algo.Length + 3] = 32;
        for (var i = 0; i < 32; i++)
            blob[4 + algo.Length + 4 + i] = (byte)(i + 1);

        return (pem, $"ssh-ed25519 {Convert.ToBase64String(blob)}");
    }

    [TestMethod]
    public void MihomoConfigRejectsUnsupportedIncrementalAndV1ShadowTlsAndSshClientVersion()
    {
        Assert.ThrowsExactly<MessageException>(() => InvokeMihomoBuild(new Hysteria2Server
        {
            Address = "127.0.0.1", Port = 443, Password = "pw", ProtoExtra = { Ports = "443,5000-6000", HopIntervalMax = "30" }
        }));
        Assert.ThrowsExactly<MessageException>(() => InvokeMihomoBuild(new Hysteria2Server
        {
            Address = "127.0.0.1", Port = 443, Password = "pw", ProtoExtra = { BbrProfile = "aggressive" }
        }));
        Assert.ThrowsExactly<MessageException>(() => InvokeMihomoBuild(new ShadowTLSServer
        {
            Address = "127.0.0.1", Port = 443, Password = "pw", ProtoExtra = { SsMethod = "aes-128-gcm", ShadowTlsVersion = 1 }
        }));
        Assert.ThrowsExactly<MessageException>(() => InvokeMihomoBuild(new SSHServer
        {
            Address = "127.0.0.1", Port = 22, Username = "root", Password = "pw", ProtoExtra = { SshClientVersion = "SSH-2.0-OpenSSH_9.6" }
        }));
    }

    [TestMethod]
    public void MihomoShadowTlsPluginOptsAndSingboxHysteria2HopIntervalClampingWork()
    {
        var stls = new ShadowTLSServer
        {
            Address = "edge.example",
            Port = 443,
            Password = "ss-pw",
            AllowInsecure = true,
            ProtoExtra =
            {
                SsMethod = "chacha20-poly1305",
                ShadowTlsVersion = 3,
                ShadowTlsPassword = "stls-pw",
                Uot = true
            }
        };

        using var mihomoDoc = BuildMihomoJson(stls);
        var proxy = mihomoDoc.RootElement.GetProperty("proxies")[0];
        Assert.AreEqual("chacha20-ietf-poly1305", proxy.GetProperty("cipher").GetString());
        Assert.IsTrue(proxy.GetProperty("udp-over-tcp").GetBoolean());
        Assert.AreEqual(2, proxy.GetProperty("udp-over-tcp-version").GetInt32());
        Assert.IsFalse(proxy.TryGetProperty("skip-cert-verify", out _));
        var pluginOpts = proxy.GetProperty("plugin-opts");
        Assert.AreEqual("edge.example", pluginOpts.GetProperty("host").GetString());
        Assert.IsTrue(pluginOpts.GetProperty("skip-cert-verify").GetBoolean());

        var hy2 = new Hysteria2Server
        {
            Address = "hy2.example",
            Port = 443,
            Password = "pw",
            ProtoExtra =
            {
                Ports = "10000-20000",
                HopIntervalMax = "15"
            }
        };

        using var sbDoc = BuildSingboxJson(hy2);
        var outbound = sbDoc.RootElement.GetProperty("outbounds")[0];
        Assert.AreEqual("15s", outbound.GetProperty("hop_interval").GetString());
        Assert.AreEqual("15s", outbound.GetProperty("hop_interval_max").GetString());

        var previousDefAllowInsecure = Netch.Global.Settings.V2RayConfig.CoreBasicItem.DefAllowInsecure;
        try
        {
            Netch.Global.Settings.V2RayConfig.CoreBasicItem.DefAllowInsecure = true;
            var ttPinned = new TrustTunnelServer
            {
                Address = "tt.example",
                Port = 443,
                Cert = SelfSignedPem.Value,
                ProtoExtra = { TrustTunnelQuic = true, CongestionControl = "cubic", BbrProfile = "aggressive" }
            };
            using var ttDoc = BuildMihomoJson(ttPinned);
            var ttProxy = ttDoc.RootElement.GetProperty("proxies")[0];
            Assert.IsFalse(ttProxy.GetProperty("skip-cert-verify").GetBoolean());
            Assert.IsFalse(ttProxy.TryGetProperty("bbr-profile", out _));
        }
        finally
        {
            Netch.Global.Settings.V2RayConfig.CoreBasicItem.DefAllowInsecure = previousDefAllowInsecure;
        }

        var sqCubic = new ShadowQUICServer
        {
            Address = "sq.example",
            Port = 443,
            Username = "u",
            Password = "p",
            ProtoExtra = { CongestionControl = "cubic", BbrProfile = "aggressive" }
        };
        using var sqDoc = BuildMihomoJson(sqCubic);
        Assert.IsFalse(sqDoc.RootElement.GetProperty("proxies")[0].TryGetProperty("bbr-profile", out _));
    }
}
