using System;
using System.Linq;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Netch.Models;
using Netch.Servers;
using Netch.Utils;

namespace Tests;

[TestClass]
public class ProtocolImport
{
    private const string Uuid = "ac7f559b-d6d0-4bdc-96b1-5e0bd260779f";
    private static readonly string WgKey = Convert.ToBase64String(Enumerable.Range(1, 32).Select(n => (byte)n).ToArray());

    [TestMethod]
    public void QueryParserPreservesEqualsAndPercentEncodedValues()
    {
        var query = Netch.Utils.Utils.ParseQueryString(
            $"?publickey={WgKey}&path=%2Fws%3Fed%3D2048&literal=/ws?ed=2048&pct=100%25&plugin=obfs-local%3Bobfs%3Dhttp%3Bobfs-host%3Da.com");

        Assert.AreEqual(WgKey, query["publickey"]);
        Assert.AreEqual("/ws?ed=2048", query["path"]);
        Assert.AreEqual("/ws?ed=2048", query["literal"]);
        Assert.AreEqual("100%", query["pct"]);
        Assert.AreEqual("obfs-local;obfs=http;obfs-host=a.com", query["plugin"]);
    }

    [TestMethod]
    public void Base64SubscriptionAllowsTrailingNewlinesAndWhitespace()
    {
        const string links = "socks5://user:pass@socks.example:1080#S1\nhttp://u:p@http.example:8080#H1\n";
        var encoded = ShareLink.URLSafeBase64Encode(links);
        var wrapped = encoded[..16] + "\r\n" + encoded[16..] + "\r\n";

        var parsed = ShareLink.ParseText(wrapped, out var skipped);
        Assert.AreEqual(0, skipped);
        Assert.AreEqual(2, parsed.Count);
        Assert.AreEqual("socks.example", parsed[0].Address);
        Assert.AreEqual("http.example", parsed[1].Address);
    }

    [TestMethod]
    public void ParseTextCountsSkippedAndHandlesCaseInsensitiveSchemesAndTgSocks()
    {
        const string input = """
            VLESS://ac7f559b-d6d0-4bdc-96b1-5e0bd260779f@vless.example:443?type=raw&path=%2Frawpath&security=tls&ech=ech1&echForceQuery=full&allowInsecure=true#VL
            vless://ac7f559b-d6d0-4bdc-96b1-5e0bd260779f@missingport.example?security=tls#NoPort
            tg://socks?server=tg.example&port=1080&user=u&pass=p&uot=1#TG
            https://t.me/socks?server=tme.example&port=1081&user=u2&pass=p2#TME
            unknown://host:443#Unknown
            """;

        var servers = ShareLink.ParseText(input, out var skipped);
        Assert.AreEqual(2, skipped);
        Assert.AreEqual(3, servers.Count);

        var vless = (VLESSServer)servers[0];
        Assert.AreEqual("tcp", vless.Network);
        Assert.AreEqual("/rawpath", vless.Path);
        Assert.AreEqual("ech1", vless.EchConfigList);
        Assert.AreEqual("full", vless.EchForceQuery);
        Assert.AreEqual(true, vless.AllowInsecure);

        var tg = (SocksServer)servers[1];
        Assert.AreEqual("tg.example", tg.Address);
        Assert.AreEqual(1080, tg.Port);
        Assert.AreEqual("u", tg.Username);
        Assert.AreEqual("p", tg.Password);
        Assert.AreEqual(true, tg.ProtoExtra.Uot);

        var tme = (SocksServer)servers[2];
        Assert.AreEqual("tme.example", tme.Address);
        Assert.AreEqual(1081, tme.Port);
    }

    [TestMethod]
    public void AnyTlsAndHysteria2UriRoundTripAllParameters()
    {
        var anyTlsLink = "anytls://secret@anytls.example/?sni=sni.example&fp=firefox&idle-session-check-interval=30&idle-session-timeout=45&min-idle-session=2#Any";
        var anyTls = (AnyTLSServer)ShareLink.ParseText(anyTlsLink).Single();
        Assert.AreEqual(443, anyTls.Port);
        Assert.AreEqual("firefox", anyTls.Fingerprint);
        Assert.AreEqual(30, anyTls.ProtoExtra.AnyTlsIdleSessionCheckIntervalSeconds);
        Assert.AreEqual(45, anyTls.ProtoExtra.AnyTlsIdleSessionTimeoutSeconds);
        Assert.AreEqual(2, anyTls.ProtoExtra.AnyTlsMinIdleSession);
        var anyTlsRoundTrip = (AnyTLSServer)ShareLink.ParseText(ShareLink.GetShareLink(anyTls)).Single();
        Assert.AreEqual(30, anyTlsRoundTrip.ProtoExtra.AnyTlsIdleSessionCheckIntervalSeconds);
        Assert.AreEqual(45, anyTlsRoundTrip.ProtoExtra.AnyTlsIdleSessionTimeoutSeconds);
        Assert.AreEqual(2, anyTlsRoundTrip.ProtoExtra.AnyTlsMinIdleSession);
        Assert.AreEqual("firefox", anyTlsRoundTrip.Fingerprint);

        var hy2Link = "hysteria2://auth@[2001:db8::1]:443,5000-6000/?obfs=gecko&obfs-password=gpass&obfs-min-packet-size=128&obfs-max-packet-size=512&hop-interval=15&upmbps=100&downmbps=200&fp=chrome#HY2";
        var hy2 = (Hysteria2Server)ShareLink.ParseText(hy2Link).Single();
        Assert.AreEqual("2001:db8::1", hy2.Address);
        Assert.AreEqual(443, hy2.Port);
        Assert.AreEqual("443,5000-6000", hy2.ProtoExtra.Ports);
        Assert.AreEqual("15", hy2.ProtoExtra.HopInterval);
        Assert.AreEqual(100, hy2.ProtoExtra.UpMbps);
        Assert.AreEqual(200, hy2.ProtoExtra.DownMbps);
        Assert.AreEqual("gecko", hy2.ProtoExtra.Obfs);
        Assert.AreEqual("gpass", hy2.ProtoExtra.SalamanderPass);
        Assert.AreEqual("128", hy2.ProtoExtra.GeckoMinPacketSize);
        Assert.AreEqual("512", hy2.ProtoExtra.GeckoMaxPacketSize);
        Assert.AreEqual("chrome", hy2.Fingerprint);

        var hy2RoundTrip = (Hysteria2Server)ShareLink.ParseText(ShareLink.GetShareLink(hy2)).Single();
        Assert.AreEqual("gecko", hy2RoundTrip.ProtoExtra.Obfs);
        Assert.AreEqual("128", hy2RoundTrip.ProtoExtra.GeckoMinPacketSize);
        Assert.AreEqual("512", hy2RoundTrip.ProtoExtra.GeckoMaxPacketSize);
        Assert.AreEqual("15", hy2RoundTrip.ProtoExtra.HopInterval);
        Assert.AreEqual(100, hy2RoundTrip.ProtoExtra.UpMbps);
        Assert.AreEqual(200, hy2RoundTrip.ProtoExtra.DownMbps);
    }

    [TestMethod]
    public void NaiveMasqueTuicHttpAndSocksUriSupportAllParameters()
    {
        var naive = (NaiveServer)ShareLink.ParseText(
            "naive+quic://u:p%3A1@naive.example?sni=n.example&insecure-concurrency=4&quic-congestion-control=bbr2&uot=1#N").Single();
        Assert.AreEqual(443, naive.Port);
        Assert.AreEqual("u", naive.Username);
        Assert.AreEqual("p:1", naive.Password);
        Assert.AreEqual(true, naive.ProtoExtra.NaiveQuic);
        Assert.AreEqual(4, naive.ProtoExtra.InsecureConcurrency);
        Assert.AreEqual("bbr2", naive.ProtoExtra.CongestionControl);
        Assert.AreEqual(true, naive.ProtoExtra.Uot);

        var masque = (MASQUEServer)ShareLink.ParseText(
            "masque://masque.example/custom/template?fp=safari&headers=X-Key%3Da%3Db#M").Single();
        Assert.AreEqual(443, masque.Port);
        Assert.AreEqual("/custom/template", masque.ProtoExtra.MasquePath);
        Assert.AreEqual("X-Key=a=b", masque.ProtoExtra.MasqueHeaders);
        Assert.AreEqual("safari", masque.Fingerprint);
        Assert.IsFalse(ShareLink.GetShareLink(masque).Contains(":@"));

        var tuic = (TUICServer)ShareLink.ParseText(
            $"tuic://{Uuid}:pw@tuic.example:443?congestion_control=bbr&udp_relay_mode=quic&reduce_rtt=1&disable_sni=1&fp=chrome#T").Single();
        Assert.IsNull(tuic.AllowInsecure);
        Assert.AreEqual("quic", tuic.ProtoExtra.TuicUdpRelayMode);
        Assert.AreEqual(true, tuic.ProtoExtra.TuicReduceRtt);
        Assert.AreEqual(true, tuic.ProtoExtra.TuicDisableSni);
        Assert.AreEqual("chrome", tuic.Fingerprint);

        var https = (HttpServer)ShareLink.ParseText(
            "https://u:p@http.example:8443?sni=h.example&alpn=h2&fp=edge&allowInsecure=true&headers=Cookie%3A%20a%3D1#H").Single();
        Assert.AreEqual("tls", https.StreamSecurity);
        Assert.AreEqual("h.example", https.Sni);
        Assert.AreEqual("h2", https.Alpn);
        Assert.AreEqual("edge", https.Fingerprint);
        Assert.AreEqual(true, https.AllowInsecure);
        Assert.AreEqual("Cookie: a=1", https.ProtoExtra.HttpHeaders);

        var socks = (SocksServer)ShareLink.ParseText("socks5h://u:p@socks.example:1080?uot=1#S").Single();
        Assert.AreEqual(true, socks.ProtoExtra.Uot);
    }

    [TestMethod]
    public void SsSsdSsrTrojanVmessAndWireGuardEdgeCasesParseCleanly()
    {
        var ssdJson = """
            {
              "airport": "Air",
              "port": 8388,
              "encryption": "aes-256-gcm",
              "password": "pw",
              "plugin": "obfs",
              "plugin_options": "obfs=http;obfs-host=cdn.example",
              "servers": [
                { "server": "s1.example", "remarks": "S1" },
                { "server": "s2.example", "port": 8389, "encryption": "none", "password": "" }
              ]
            }
            """;
        var ssdNodes = ShareLink.ParseText("ssd://" + ShareLink.ToBase64String(ssdJson)).Cast<ShadowsocksServer>().ToArray();
        Assert.AreEqual(2, ssdNodes.Length);
        Assert.AreEqual("obfs-local", ssdNodes[0].ProtoExtra.SsPlugin);
        Assert.AreEqual("Air", ssdNodes[1].Remarks);
        Assert.AreEqual(8389, ssdNodes[1].Port);

        var ssMux = (ShadowsocksServer)ShareLink.ParseText(
            "ss://YWVzLTEyOC1nY206cGFzcw@ss.example:443?plugin=v2ray-plugin%3Bmode%3Dwebsocket%3Bmux%3D1&uot=1#SS").Single();
        Assert.AreEqual(true, ssMux.ProtoExtra.Uot);

        var ssLegacy = (ShadowsocksServer)ShareLink.ParseText(
            "ss://" + ShareLink.ToBase64String("aes-128-gcm:pw@[2001:db8::1]:443") + "#HK Node 01").Single();
        Assert.AreEqual("2001:db8::1", ssLegacy.Address);
        Assert.AreEqual("HK Node 01", ssLegacy.Remarks);

        var ssrNoSlash = (ShadowsocksRServer)ShareLink.ParseText(
            "ssr://" + ShareLink.URLSafeBase64Encode("1.2.3.4:443:origin:aes-256-cfb:plain:" + ShareLink.URLSafeBase64Encode("pw") + "?remarks=" + ShareLink.URLSafeBase64Encode("R"))).Single();
        Assert.AreEqual("R", ssrNoSlash.Remarks);

        var trojan = (TrojanServer)ShareLink.ParseText(
            "trojan://pw@tr.example?peer=peer.example&security=none#TR").Single();
        Assert.AreEqual(443, trojan.Port);
        Assert.AreEqual("peer.example", trojan.Sni);
        Assert.AreEqual("none", trojan.StreamSecurity);

        var stdVmess = (VMessServer)ShareLink.ParseText(
            $"vmess://aes-128-gcm:{Uuid}@vm.example:443?aid=2&type=tcp#VM").Single();
        Assert.AreEqual("aes-128-gcm", stdVmess.ProtoExtra.VmessSecurity);
        Assert.AreEqual(2, stdVmess.ProtoExtra.AlterId);
        Assert.AreEqual(Uuid, stdVmess.Password);

        var jsonVmess = new
        {
            v = 2,
            ps = "JV",
            add = "vm2.example",
            port = 443,
            id = Uuid,
            aid = 0,
            net = "http",
            tls = "tls",
            insecure = true,
            extra = "{\"xPaddingBytes\":\"100\"}",
            ech = "ech-val",
            echForceQuery = "half",
            pcs = "sha"
        };
        var vmessFromJson = (VMessServer)ShareLink.ParseText("vmess://" + ShareLink.ToBase64String(JsonSerializer.Serialize(jsonVmess))).Single();
        Assert.AreEqual("h2", vmessFromJson.Network);
        Assert.AreEqual(true, vmessFromJson.AllowInsecure);
        Assert.AreEqual("{\"xPaddingBytes\":\"100\"}", vmessFromJson.Extra);
        Assert.AreEqual("ech-val", vmessFromJson.EchConfigList);
        Assert.AreEqual("half", vmessFromJson.EchForceQuery);
        Assert.AreEqual("sha", vmessFromJson.CertSha);

        var wgReservedB64 = Convert.ToBase64String(new byte[] { 10, 20, 30 });
        var wgLink = $"wg://{Uri.EscapeDataString(WgKey)}@wg.example:51820?pbk={WgKey}&psk={WgKey}&ip=10.0.0.2/32&reserved={wgReservedB64}#WG";
        var wg = (WireGuardServer)ShareLink.ParseText(wgLink).Single();
        Assert.AreEqual(WgKey, wg.ProtoExtra.WgPublicKey);
        Assert.AreEqual(WgKey, wg.ProtoExtra.WgPresharedKey);
        Assert.AreEqual("10.0.0.2/32", wg.ProtoExtra.WgInterfaceAddress);
        Assert.AreEqual("10,20,30", wg.ProtoExtra.WgReserved);
    }

    [TestMethod]
    public void ClashYamlSupportsExtendedProtocolsAndOptions()
    {
        const string yaml = """
            proxies:
              - name: VMess KCP
                type: vmess
                server: vm.example
                port: 443
                uuid: ac7f559b-d6d0-4bdc-96b1-5e0bd260779f
                alterId: 0
                cipher: auto
                network: mkcp
                mkcp-opts:
                  header: srtp
                  seed: my-seed
              - name: Trojan XHTTP
                type: trojan
                server: tr.example
                port: 443
                password: pass
                tls: false
                network: xhttp
                xhttp-opts:
                  path: /xh
                  mode: packet-up
                  extra:
                    xPaddingBytes: 100-200
              - name: HY2 PortsOnly
                type: hysteria2
                server: hy2.example
                ports: 443,8443-8445
                password: pass
                up: 100 Mbps
                down: 1 Gbps
              - name: TUIC Ext
                type: tuic
                server: tuic.example
                port: 443
                uuid: ac7f559b-d6d0-4bdc-96b1-5e0bd260779f
                password: pass
                udp-relay-mode: quic
                reduce-rtt: true
                disable-sni: true
              - name: WG Peers
                type: wireguard
                ip: 10.0.0.2/32
                private-key: __KEY__
                peers:
                  - server: wg.example
                    port: 51820
                    public-key: __KEY__
                    reserved: AQID
              - name: Naive Node
                type: naive
                server: naive.example
                port: 443
                username: u
                password: p
                quic: true
                quic-congestion-control: bbr
                insecure-concurrency: 2
                udp-over-tcp: true
              - name: MASQUE Node
                type: masque
                server: masque.example
                port: 443
                path: /.well-known/masque/ip/{target}/{ipproto}/
                headers:
                  X-Test: val
              - name: SS QUIC
                type: ss
                server: ss.example
                port: 443
                cipher: aes-128-gcm
                password: pass
                plugin: v2ray-plugin
                plugin-opts:
                  mode: quic
            """;

        var servers = ShareLink.ParseText(yaml.Replace("__KEY__", WgKey), out var skipped);
        Assert.AreEqual(0, skipped);
        Assert.AreEqual(8, servers.Count);

        Assert.AreEqual("kcp", servers[0].Network);
        Assert.AreEqual("srtp", servers[0].HeaderType);
        Assert.AreEqual("my-seed", servers[0].Path);

        Assert.AreEqual("none", servers[1].StreamSecurity);
        Assert.AreEqual("xhttp", servers[1].Network);
        Assert.IsTrue(servers[1].Extra.Contains("xPaddingBytes"));

        Assert.AreEqual(443, servers[2].Port);
        Assert.AreEqual(100, servers[2].ProtoExtra.UpMbps);
        Assert.AreEqual(1000, servers[2].ProtoExtra.DownMbps);

        Assert.AreEqual("quic", servers[3].ProtoExtra.TuicUdpRelayMode);
        Assert.AreEqual(true, servers[3].ProtoExtra.TuicReduceRtt);
        Assert.AreEqual(true, servers[3].ProtoExtra.TuicDisableSni);

        Assert.AreEqual("wg.example", servers[4].Address);
        Assert.AreEqual(51820, servers[4].Port);
        Assert.AreEqual("1,2,3", servers[4].ProtoExtra.WgReserved);

        Assert.AreEqual(true, servers[5].ProtoExtra.NaiveQuic);
        Assert.AreEqual("bbr", servers[5].ProtoExtra.CongestionControl);
        Assert.AreEqual(2, servers[5].ProtoExtra.InsecureConcurrency);
        Assert.AreEqual(true, servers[5].ProtoExtra.Uot);

        Assert.AreEqual("/.well-known/masque/ip/{target}/{ipproto}/", servers[6].ProtoExtra.MasquePath);
        Assert.AreEqual("X-Test: val", servers[6].ProtoExtra.MasqueHeaders);

        Assert.AreEqual("quic", servers[7].Network);
    }

    [TestMethod]
    public void ServerValidatorRejectsInvalidFieldsAndAcceptsValidNodes()
    {
        var badSocks = new SocksServer("127.0.0.1", 1080) { Username = "only-user" };
        Assert.IsNotNull(ServerValidator.Validate(badSocks));

        var badHy2 = new Hysteria2Server
        {
            Address = "127.0.0.1", Port = 443,
            ProtoExtra = { Obfs = "gecko", SalamanderPass = "p", GeckoMinPacketSize = "500", GeckoMaxPacketSize = "100" }
        };
        Assert.IsNotNull(ServerValidator.Validate(badHy2));

        var badVless = new VLESSServer
        {
            Address = "127.0.0.1", Port = 443, Password = Uuid,
            Network = "ws", StreamSecurity = "reality", PublicKey = "pubkey"
        };
        Assert.IsNotNull(ServerValidator.Validate(badVless));

        var goodVless = new VLESSServer
        {
            Address = "127.0.0.1", Port = 443, Password = Uuid,
            Network = "xhttp", StreamSecurity = "reality", PublicKey = "pubkey", ShortId = "abcd",
            Extra = """{"xPaddingBytes":"100"}"""
        };
        Assert.IsNull(ServerValidator.Validate(goodVless));
    }

    [TestMethod]
    public void AllThirteenProtocolsRoundTripThroughShareLinks()
    {
        var ss16Key = Convert.ToBase64String(Enumerable.Range(1, 16).Select(n => (byte)n).ToArray());

        var vlessReality = new VLESSServer
        {
            Remarks = "VL Reality", Address = "vless.example", Port = 8443, Password = Uuid,
            Network = "xhttp", HeaderType = "packet-up", RequestHost = "xh.example", Path = "/xhttp",
            Extra = """{"xPaddingBytes":"100-200"}""",
            StreamSecurity = "reality", Sni = "sni.example", Fingerprint = "chrome",
            PublicKey = "pubkey123", ShortId = "abcd1234", SpiderX = "/sp", Mldsa65Verify = "mlkey",
            ProtoExtra = { Flow = "xtls-rprx-vision-udp443", VlessEncryption = "mlkem768x25519plus.native.0rtt.sample" }
        };
        var vlRt = (VLESSServer)ShareLink.ParseText(ShareLink.GetShareLink(vlessReality)).Single();
        Assert.AreEqual("VL Reality", vlRt.Remarks);
        Assert.AreEqual(8443, vlRt.Port);
        Assert.AreEqual("xtls-rprx-vision-udp443", vlRt.ProtoExtra.Flow);
        Assert.AreEqual("mlkem768x25519plus.native.0rtt.sample", vlRt.ProtoExtra.VlessEncryption);
        Assert.AreEqual("xhttp", vlRt.Network);
        Assert.AreEqual("packet-up", vlRt.HeaderType);
        Assert.AreEqual("xh.example", vlRt.RequestHost);
        Assert.AreEqual("/xhttp", vlRt.Path);
        Assert.IsTrue(System.Text.Json.Nodes.JsonNode.DeepEquals(
            System.Text.Json.Nodes.JsonNode.Parse("""{"xPaddingBytes":"100-200"}"""),
            System.Text.Json.Nodes.JsonNode.Parse(vlRt.Extra)));
        Assert.AreEqual("reality", vlRt.StreamSecurity);
        Assert.AreEqual("sni.example", vlRt.Sni);
        Assert.AreEqual("chrome", vlRt.Fingerprint);
        Assert.AreEqual("pubkey123", vlRt.PublicKey);
        Assert.AreEqual("abcd1234", vlRt.ShortId);
        Assert.AreEqual("/sp", vlRt.SpiderX);
        Assert.AreEqual("mlkey", vlRt.Mldsa65Verify);

        var vlessGrpc = new VLESSServer
        {
            Remarks = "VL gRPC", Address = "grpc.example", Port = 443, Password = Uuid,
            Network = "grpc", HeaderType = "multi", RequestHost = "auth.example", Path = "my-svc",
            StreamSecurity = "tls", Sni = "sni.example", Alpn = "h2", Fingerprint = "firefox",
            AllowInsecure = true, EchConfigList = "ech-cfg", EchForceQuery = "full"
        };
        var grpcRt = (VLESSServer)ShareLink.ParseText(ShareLink.GetShareLink(vlessGrpc)).Single();
        Assert.AreEqual("grpc", grpcRt.Network);
        Assert.AreEqual("multi", grpcRt.HeaderType);
        Assert.AreEqual("auth.example", grpcRt.RequestHost);
        Assert.AreEqual("my-svc", grpcRt.Path);
        Assert.AreEqual("h2", grpcRt.Alpn);
        Assert.AreEqual(true, grpcRt.AllowInsecure);
        Assert.AreEqual("ech-cfg", grpcRt.EchConfigList);
        Assert.AreEqual("full", grpcRt.EchForceQuery);

        var vmess = new VMessServer
        {
            Remarks = "VM KCP", Address = "vm.example", Port = 443, Password = Uuid,
            Network = "kcp", HeaderType = "dns", RequestHost = "dns.example", Path = "seed-val",
            StreamSecurity = "tls", Sni = "sni.example", Alpn = "h2,http/1.1", Fingerprint = "safari",
            AllowInsecure = false, EchConfigList = "ech-vm", EchForceQuery = "half", CertSha = "sha256hex",
            ProtoExtra = { AlterId = 1, VmessSecurity = "aes-128-gcm" }
        };
        var vmRt = (VMessServer)ShareLink.ParseText(ShareLink.GetShareLink(vmess)).Single();
        Assert.AreEqual(1, vmRt.ProtoExtra.AlterId);
        Assert.AreEqual("aes-128-gcm", vmRt.ProtoExtra.VmessSecurity);
        Assert.AreEqual("kcp", vmRt.Network);
        Assert.AreEqual("dns", vmRt.HeaderType);
        Assert.AreEqual("dns.example", vmRt.RequestHost);
        Assert.AreEqual("seed-val", vmRt.Path);
        Assert.AreEqual(false, vmRt.AllowInsecure);
        Assert.AreEqual("ech-vm", vmRt.EchConfigList);
        Assert.AreEqual("half", vmRt.EchForceQuery);
        Assert.AreEqual("sha256hex", vmRt.CertSha);

        var trojan = new TrojanServer
        {
            Remarks = "TR WS", Address = "tr.example", Port = 443, Password = "p@ss:1#2",
            Network = "ws", RequestHost = "ws.example", Path = "/ws?ed=2048",
            StreamSecurity = "tls", Sni = "tr-sni.example", Alpn = "http/1.1", Fingerprint = "edge",
            AllowInsecure = true
        };
        var trRt = (TrojanServer)ShareLink.ParseText(ShareLink.GetShareLink(trojan)).Single();
        Assert.AreEqual("p@ss:1#2", trRt.Password);
        Assert.AreEqual("ws", trRt.Network);
        Assert.AreEqual("ws.example", trRt.RequestHost);
        Assert.AreEqual("/ws?ed=2048", trRt.Path);
        Assert.AreEqual("tr-sni.example", trRt.Sni);
        Assert.AreEqual("http/1.1", trRt.Alpn);
        Assert.AreEqual("edge", trRt.Fingerprint);
        Assert.AreEqual(true, trRt.AllowInsecure);

        var ss2022 = new ShadowsocksServer
        {
            Remarks = "SS 2022", Address = "ss.example", Port = 8443, Password = $"{ss16Key}:{ss16Key}",
            AllowInsecure = true,
            ProtoExtra =
            {
                SsMethod = "2022-blake3-aes-128-gcm",
                SsPlugin = "v2ray-plugin",
                SsPluginOptions = @"mode=websocket;path=/a\=b;tls",
                Uot = true
            }
        };
        var ssRt = (ShadowsocksServer)ShareLink.ParseText(ShareLink.GetShareLink(ss2022)).Single();
        Assert.AreEqual("2022-blake3-aes-128-gcm", ssRt.ProtoExtra.SsMethod);
        Assert.AreEqual($"{ss16Key}:{ss16Key}", ssRt.Password);
        Assert.AreEqual("v2ray-plugin", ssRt.ProtoExtra.SsPlugin);
        Assert.AreEqual("/a=b", ssRt.Path);
        Assert.AreEqual("tls", ssRt.StreamSecurity);
        Assert.AreEqual(true, ssRt.AllowInsecure);
        Assert.AreEqual(true, ssRt.ProtoExtra.Uot);

        var ssObfs = new ShadowsocksServer
        {
            Remarks = "SS Obfs", Address = "obfs.example", Port = 443, Password = "pw",
            ProtoExtra = { SsMethod = "aes-128-gcm", SsPlugin = "obfs-local", SsPluginOptions = "obfs=tls;obfs-host=cdn.example" }
        };
        var ssObfsRt = (ShadowsocksServer)ShareLink.ParseText(ShareLink.GetShareLink(ssObfs)).Single();
        Assert.AreEqual("obfs-local", ssObfsRt.ProtoExtra.SsPlugin);
        Assert.AreEqual("obfs=tls;obfs-host=cdn.example", ssObfsRt.ProtoExtra.SsPluginOptions);

        var ssr = new ShadowsocksRServer
        {
            Remarks = "SSR", Group = "Grp", Address = "ssr.example", Port = 443, Password = "pw",
            EncryptMethod = "aes-256-cfb", Protocol = "auth_chain_a", ProtocolParam = "10:p",
            OBFS = "http_simple", OBFSParam = "obfs.example"
        };
        var ssrRt = (ShadowsocksRServer)ShareLink.ParseText(ShareLink.GetShareLink(ssr)).Single();
        Assert.AreEqual("Grp", ssrRt.Group);
        Assert.AreEqual("auth_chain_a", ssrRt.Protocol);
        Assert.AreEqual("10:p", ssrRt.ProtocolParam);
        Assert.AreEqual("http_simple", ssrRt.OBFS);
        Assert.AreEqual("obfs.example", ssrRt.OBFSParam);

        var socksAuth = new SocksServer("socks.example", 1080, "user", "p@ss:1") { Remarks = "SAuth", ProtoExtra = { Uot = true } };
        var socksAuthRt = (SocksServer)ShareLink.ParseText(ShareLink.GetShareLink(socksAuth)).Single();
        Assert.AreEqual("user", socksAuthRt.Username);
        Assert.AreEqual("p@ss:1", socksAuthRt.Password);
        Assert.AreEqual(true, socksAuthRt.ProtoExtra.Uot);

        var socksNoAuth = new SocksServer("socks-noauth.example", 1080) { Remarks = "NoAuth" };
        var socksNoAuthLink = ShareLink.GetShareLink(socksNoAuth);
        Assert.IsFalse(socksNoAuthLink.Contains('@'));
        Assert.IsFalse(socksNoAuthLink.Contains("?"));
        var socksNoAuthRt = (SocksServer)ShareLink.ParseText(socksNoAuthLink).Single();
        Assert.AreEqual("socks-noauth.example", socksNoAuthRt.Address);
        Assert.AreEqual("", socksNoAuthRt.Username ?? "");

        var http = new HttpServer("http.example", 8443, "u", "p")
        {
            Remarks = "HTTPS", StreamSecurity = "tls", Sni = "h.example", Alpn = "h2",
            Fingerprint = "chrome", AllowInsecure = true, ProtoExtra = { HttpHeaders = "X-Key: val" }
        };
        var httpLink = ShareLink.GetShareLink(http);
        StringAssert.StartsWith(httpLink, "Netch://");
        var httpRt = (HttpServer)ShareLink.ParseText(httpLink).Single();
        Assert.AreEqual("tls", httpRt.StreamSecurity);
        Assert.AreEqual("h2", httpRt.Alpn);
        Assert.AreEqual("chrome", httpRt.Fingerprint);
        Assert.AreEqual(true, httpRt.AllowInsecure);
        Assert.AreEqual("X-Key: val", httpRt.ProtoExtra.HttpHeaders);

        var hy2 = new Hysteria2Server
        {
            Remarks = "HY2", Address = "hy2.example", Port = 443, Password = "pw",
            StreamSecurity = "tls", Sni = "hy2-sni.example", Alpn = "h3", Fingerprint = "chrome",
            AllowInsecure = true, CertSha = "pinhex",
            ProtoExtra =
            {
                Obfs = "gecko", SalamanderPass = "gpw", GeckoMinPacketSize = "128", GeckoMaxPacketSize = "512",
                Ports = "443,5000-6000", HopInterval = "15", UpMbps = 50, DownMbps = 200
            }
        };
        var hy2Rt = (Hysteria2Server)ShareLink.ParseText(ShareLink.GetShareLink(hy2)).Single();
        Assert.AreEqual("gecko", hy2Rt.ProtoExtra.Obfs);
        Assert.AreEqual("gpw", hy2Rt.ProtoExtra.SalamanderPass);
        Assert.AreEqual("128", hy2Rt.ProtoExtra.GeckoMinPacketSize);
        Assert.AreEqual("512", hy2Rt.ProtoExtra.GeckoMaxPacketSize);
        Assert.AreEqual("443,5000-6000", hy2Rt.ProtoExtra.Ports);
        Assert.AreEqual("15", hy2Rt.ProtoExtra.HopInterval);
        Assert.AreEqual(50, hy2Rt.ProtoExtra.UpMbps);
        Assert.AreEqual(200, hy2Rt.ProtoExtra.DownMbps);
        Assert.AreEqual("pinhex", hy2Rt.CertSha);
        Assert.AreEqual(true, hy2Rt.AllowInsecure);

        var tuic = new TUICServer
        {
            Remarks = "TUIC", Address = "tuic.example", Port = 443, Username = Uuid, Password = "pw",
            Sni = "tuic-sni.example", Alpn = "h3", Fingerprint = "firefox", AllowInsecure = true,
            ProtoExtra = { CongestionControl = "bbr", TuicUdpRelayMode = "quic", TuicReduceRtt = true, TuicDisableSni = true }
        };
        var tuicRt = (TUICServer)ShareLink.ParseText(ShareLink.GetShareLink(tuic)).Single();
        Assert.AreEqual(Uuid, tuicRt.Username);
        Assert.AreEqual("bbr", tuicRt.ProtoExtra.CongestionControl);
        Assert.AreEqual("quic", tuicRt.ProtoExtra.TuicUdpRelayMode);
        Assert.AreEqual(true, tuicRt.ProtoExtra.TuicReduceRtt);
        Assert.AreEqual(true, tuicRt.ProtoExtra.TuicDisableSni);
        Assert.AreEqual("firefox", tuicRt.Fingerprint);
        Assert.AreEqual(true, tuicRt.AllowInsecure);
        Assert.AreEqual(false, ShareLink.ParseText($"tuic://{Uuid}:pw@tuic.example:443?insecure=0").Single().AllowInsecure);

        var anyTls = new AnyTLSServer
        {
            Remarks = "AnyTLS", Address = "any.example", Port = 443, Password = "p:1",
            Sni = "any-sni.example", Alpn = "h2", Fingerprint = "safari", AllowInsecure = true,
            ProtoExtra = { AnyTlsIdleSessionCheckIntervalSeconds = 30, AnyTlsIdleSessionTimeoutSeconds = 45, AnyTlsMinIdleSession = 2 }
        };
        var anyRt = (AnyTLSServer)ShareLink.ParseText(ShareLink.GetShareLink(anyTls)).Single();
        Assert.AreEqual("p:1", anyRt.Password);
        Assert.AreEqual("h2", anyRt.Alpn);
        Assert.AreEqual("safari", anyRt.Fingerprint);
        Assert.AreEqual(true, anyRt.AllowInsecure);
        Assert.AreEqual(30, anyRt.ProtoExtra.AnyTlsIdleSessionCheckIntervalSeconds);
        Assert.AreEqual(45, anyRt.ProtoExtra.AnyTlsIdleSessionTimeoutSeconds);
        Assert.AreEqual(2, anyRt.ProtoExtra.AnyTlsMinIdleSession);

        var wg = new WireGuardServer
        {
            Remarks = "WG", Address = "wg.example", Port = 51820, Password = WgKey,
            ProtoExtra =
            {
                WgPublicKey = WgKey, WgPresharedKey = WgKey,
                WgInterfaceAddress = "10.0.0.2/32,fd00::2/128", WgReserved = "1,2,3", WgMtu = 1408
            }
        };
        var wgRt = (WireGuardServer)ShareLink.ParseText(ShareLink.GetShareLink(wg)).Single();
        Assert.AreEqual(WgKey, wgRt.Password);
        Assert.AreEqual(WgKey, wgRt.ProtoExtra.WgPublicKey);
        Assert.AreEqual(WgKey, wgRt.ProtoExtra.WgPresharedKey);
        Assert.AreEqual("10.0.0.2/32,fd00::2/128", wgRt.ProtoExtra.WgInterfaceAddress);
        Assert.AreEqual("1,2,3", wgRt.ProtoExtra.WgReserved);
        Assert.AreEqual(1408, wgRt.ProtoExtra.WgMtu);

        var naive = new NaiveServer
        {
            Remarks = "Naive", Address = "naive.example", Port = 443, Username = "u", Password = "p:1@2",
            Sni = "naive-sni.example",
            ProtoExtra = { NaiveQuic = true, InsecureConcurrency = 3, CongestionControl = "bbr2", Uot = true }
        };
        var naiveLink = ShareLink.GetShareLink(naive);
        Assert.IsFalse(naiveLink.Contains("%253A"));
        var naiveRt = (NaiveServer)ShareLink.ParseText(naiveLink).Single();
        Assert.AreEqual("p:1@2", naiveRt.Password);
        Assert.AreEqual(true, naiveRt.ProtoExtra.NaiveQuic);
        Assert.AreEqual(3, naiveRt.ProtoExtra.InsecureConcurrency);
        Assert.AreEqual("bbr2", naiveRt.ProtoExtra.CongestionControl);
        Assert.AreEqual(true, naiveRt.ProtoExtra.Uot);

        foreach (var hasAuth in new[] { false, true })
        {
            var masque = new MASQUEServer
            {
                Remarks = "MASQUE", Address = "masque.example", Port = 443,
                Username = hasAuth ? "mu" : "", Password = hasAuth ? "mp:1" : "",
                Sni = "m-sni.example", Alpn = "h3", Fingerprint = "chrome", AllowInsecure = true,
                ProtoExtra = { MasquePath = "/.well-known/masque/ip/{target}/{ipproto}/", MasqueHeaders = "X-A: 1" }
            };
            var masqueRt = (MASQUEServer)ShareLink.ParseText(ShareLink.GetShareLink(masque)).Single();
            Assert.AreEqual(hasAuth ? "mu" : null, masqueRt.Username);
            Assert.AreEqual(hasAuth ? "mp:1" : "", masqueRt.Password);
            Assert.AreEqual("/.well-known/masque/ip/{target}/{ipproto}/", masqueRt.ProtoExtra.MasquePath);
            Assert.AreEqual("X-A: 1", masqueRt.ProtoExtra.MasqueHeaders);
            Assert.AreEqual(true, masqueRt.AllowInsecure);
        }
    }

    [TestMethod]
    public void MalformedUrisAndInvalidNodesAreRejected()
    {
        var invalidLines = string.Join('\n', new[]
        {
            $"vless://{Uuid}@h.example?security=tls#NoPort",
            $"vmess://auto:{Uuid}@h.example?type=tcp#NoPort",
            "ss://YWVzLTEyOC1nY206cHc@h.example#NoPort",
            "socks5://h.example#NoPort",
            $"wireguard://{Uri.EscapeDataString(WgKey)}@h.example?publickey={WgKey}&address=10.0.0.2/32#NoPort",
            $"vless://{Uuid}@h.example:0#ZeroPort",
            $"vless://{Uuid}@h.example:65536#HighPort",
            "socks5://onlyuser@socks.example:1080#PartialAuth",
            "tg://socks?server=tg.example&port=1080&user=onlyuser#TgPartial",
            "ss://dW5rbm93bjpwdw@ss.example:443#BadCipher",
            $"ss://2022-blake3-aes-128-gcm:{Uri.EscapeDataString(WgKey)}@ss.example:443#Bad2022KeyLen",
            $"wireguard://badkey@wg.example:51820?publickey={WgKey}&address=10.0.0.2/32#BadWgKey",
            $"wireguard://{Uri.EscapeDataString(WgKey)}@wg.example:51820?publickey={WgKey}#MissingWgAddr",
            $"wireguard://{Uri.EscapeDataString(WgKey)}@wg.example:51820?publickey={WgKey}&address=10.0.0.2/32&reserved=1,2,300#BadWgReserved",
            "tuic://not-a-uuid:pw@tuic.example:443#BadTuicUuid",
            $"tuic://{Guid.Empty}:pw@tuic.example:443#EmptyTuicUuid",
            "anytls://@anytls.example:443#NoPass",
            "naive+https://@naive.example:443#NoPass",
            "socks5://onlyuser:@socks.example:1080#EmptyPass",
            "vmess://!!!",
            "ssd://" + ShareLink.ToBase64String("""{"servers":[]}""")
        });

        var parsed = ShareLink.ParseText(invalidLines, out var skipped);
        Assert.AreEqual(0, parsed.Count);
        Assert.AreEqual(21, skipped);

        var percentFallback = (SocksServer)ShareLink.ParseText("socks5://u:p%ZZ@h.example:1080?x=%2#bad%GG", out var pctSkipped).Single();
        Assert.AreEqual(0, pctSkipped);
        Assert.AreEqual("p%ZZ", percentFallback.Password);

        var encodedSsKey = Convert.ToBase64String(Enumerable.Range(1, 16).Select(n => (byte)n).ToArray());
        var encodedUriText = string.Join('\n',
        [
            $"ss://2022-blake3-aes-128-gcm%3A{Uri.EscapeDataString(encodedSsKey)}@ss.example:443#SsEncodedColon",
            "socks5://user%3Apass%2550@socks.example:1080#SocksEncodedColon"
        ]);
        var encodedColonServers = ShareLink.ParseText(encodedUriText, out var encodedColonSkipped);
        Assert.AreEqual(0, encodedColonSkipped);
        Assert.AreEqual(2, encodedColonServers.Count);
        var encodedSs = encodedColonServers[0];
        Assert.AreEqual("2022-blake3-aes-128-gcm", encodedSs.ProtoExtra.SsMethod);
        Assert.AreEqual(encodedSsKey, encodedSs.Password);
        var encodedSocks = (SocksServer)encodedColonServers[1];
        Assert.AreEqual("user", encodedSocks.Username);
        Assert.AreEqual("pass%50", encodedSocks.Password);

        var defaults = ShareLink.ParseText(string.Join('\n', new[]
        {
            "http://h.example",
            "https://h.example",
            "trojan://pw@tr.example",
            "hy2://pw@hy.example",
            $"tuic://{Uuid}:pw@tuic.example:443",
            "anytls://pw@any.example",
            "naive+https://u:p@naive.example",
            "masque://masque.example"
        }), out var defaultsSkipped);
        Assert.AreEqual(0, defaultsSkipped);
        CollectionAssert.AreEqual(new[] { 80, 443, 443, 443, 443, 443, 443, 443 }, defaults.Select(s => s.Port).ToArray());

        Assert.AreEqual(Netch.Enums.EConfigType.VLESS.ToString(), ServerHelper.GetUtilByUriScheme("VLESS")!.TypeName);
        Assert.AreEqual(Netch.Enums.EConfigType.Hysteria2.ToString(), ServerHelper.GetUtilByUriScheme("HY2")!.TypeName);
        Assert.AreEqual(Netch.Enums.EConfigType.Naive.ToString(), ServerHelper.GetUtilByUriScheme("Naive+Quic")!.TypeName);
        Assert.AreEqual(Netch.Enums.EConfigType.WireGuard.ToString(), ServerHelper.GetUtilByUriScheme("WG")!.TypeName);
        Assert.AreEqual(Netch.Enums.EConfigType.HTTP.ToString(), ServerHelper.GetUtilByUriScheme("HTTPS")!.TypeName);
        Assert.AreEqual(Netch.Enums.EConfigType.AnyTLS.ToString(), ServerHelper.GetUtilByTypeName("Anytls").TypeName);
    }

    [TestMethod]
    public void ClashYamlHandlesAliasesAndRejectsIncompatibleOptions()
    {
        const string yaml = """
            proxies:
              - name: VLESS Aliases
                type: vless
                server: vless.example
                port: 443
                uuid: ac7f559b-d6d0-4bdc-96b1-5e0bd260779f
                tls: true
                sni: alias.example
                certificate: "-----BEGIN CERTIFICATE-----\nMII=\n-----END CERTIFICATE-----"
                ech-opts:
                  enable: true
                  config: ech-yaml
                network: xhttp
                xhttp-opts:
                  path: /xh
                  mode: stream-up
                  extra: '{"xPaddingBytes":"10-20"}'
              - name: VLESS Reality SpiderX
                type: vless
                server: r.example
                port: 443
                uuid: ac7f559b-d6d0-4bdc-96b1-5e0bd260779f
                tls: true
                servername: sni.example
                reality-opts:
                  public-key: pubkey
                  short-id: abcd
                  spider-x: /custom
              - name: Conflicting SNI
                type: trojan
                server: bad.example
                port: 443
                password: pw
                sni: one.example
                servername: two.example
              - name: Trojan H2 Plain
                type: trojan
                server: tr-h2.example
                port: 443
                password: pw
                tls: false
                network: h2
                h2-opts:
                  host: [h1.example, h2.example]
                  path: /h2
              - name: Trojan QUIC
                type: trojan
                server: tr-q.example
                port: 443
                password: pw
                network: quic
              - name: HY2 Units
                type: hysteria2
                server: hy.example
                port: 443
                password: pw
                up: 500 Kbps
                down: 50M
                hop-interval: 15
              - name: SS SimpleObfs
                type: ss
                server: ss1.example
                port: 443
                cipher: aes-128-gcm
                password: pw
                plugin: simple-obfs
                plugin-opts:
                  mode: http
                  host: cdn.example
              - name: SS V2Ray SkipVerify
                type: ss
                server: ss2.example
                port: 443
                cipher: aes-128-gcm
                password: pw
                plugin: v2ray-plugin
                plugin-opts:
                  mode: websocket
                  skip-cert-verify: true
              - name: Socks NoTls
                type: socks5
                server: socks-ok.example
                port: 1080
                tls: false
              - name: Socks TlsRejected
                type: socks5
                server: socks-bad.example
                port: 1080
                tls: true
              - name: WARP Masque Rejected
                type: masque
                server: warp.example
                port: 443
                private-key: warp-key
              - name: Hy2 Zero Hop Rejected
                type: hysteria2
                server: hy2zero.example
                port: 443
                password: secret
                ports: 20000-20010
                hop-interval: 0
            """;

        var servers = ShareLink.ParseText(yaml, out var skipped);
        Assert.AreEqual(4, skipped);
        Assert.AreEqual(8, servers.Count);

        var vlAlias = (VLESSServer)servers[0];
        Assert.AreEqual("alias.example", vlAlias.Sni);
        Assert.IsTrue(vlAlias.Cert.Contains("BEGIN CERTIFICATE"));
        Assert.AreEqual("ech-yaml", vlAlias.EchConfigList);
        Assert.AreEqual("stream-up", vlAlias.HeaderType);
        Assert.AreEqual("{\"xPaddingBytes\":\"10-20\"}", vlAlias.Extra);

        var vlReality = (VLESSServer)servers[1];
        Assert.AreEqual("reality", vlReality.StreamSecurity);
        Assert.AreEqual("/custom", vlReality.SpiderX);

        var trH2 = (TrojanServer)servers[2];
        Assert.AreEqual("none", trH2.StreamSecurity);
        Assert.AreEqual("h2", trH2.Network);
        Assert.AreEqual("h1.example,h2.example", trH2.RequestHost);
        Assert.AreEqual("/h2", trH2.Path);

        Assert.AreEqual("quic", servers[3].Network);

        Assert.AreEqual(1, servers[4].ProtoExtra.UpMbps);
        Assert.AreEqual(50, servers[4].ProtoExtra.DownMbps);
        Assert.IsNull(servers[4].ProtoExtra.HopInterval);

        Assert.AreEqual("obfs-local", servers[5].ProtoExtra.SsPlugin);
        Assert.AreEqual("obfs=http;obfs-host=cdn.example", servers[5].ProtoExtra.SsPluginOptions);

        Assert.AreEqual(true, servers[6].AllowInsecure);

        Assert.AreEqual("socks-ok.example", servers[7].Address);
    }
}
