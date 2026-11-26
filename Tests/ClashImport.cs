using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Netch.JsonConverter;
using Netch.Models;
using Netch.Servers;
using Netch.Utils;

namespace Tests;

[TestClass]
public class ClashImport
{
    private const string ProxyList = """
        port: 7890
        shared: &shared {name: "香港, 01", type: ss, server: ss.example, port: 443, cipher: aes-128-gcm, password: "00: #pass", plugin: obfs, plugin-opts: {mode: tls, host: cdn.example}}
        proxy-providers:
          remote: {type: http, url: "https://provider.example/nodes.yaml"}
        proxies:
          - *shared
          - name: VLESS WS
            type: vless
            server: vless.example
            port: 443
            uuid: 00000000-0000-0000-0000-000000000001
            tls: true
            servername: tls.example
            alpn: [h2, http/1.1]
            skip-cert-verify: false
            client-fingerprint: chrome
            network: ws
            packet-encoding: xudp
            ws-opts:
              path: /ws
              headers: {Host: ws.example}
              max-early-data: 2048
              early-data-header-name: Sec-WebSocket-Protocol
          - {name: Unsupported, type: snell, server: snell.example, port: 443}
          - {name: Invalid, type: socks5, server: socks.example, port: 65536}
          - {name: Chained, type: socks5, server: socks.example, port: 1080, dialer-proxy: "香港, 01"}
        rules: [MATCH,DIRECT]
        """;

    [TestMethod]
    public void ImportsValidNativeNodesFromClashYaml()
    {
        var servers = ShareLink.ParseText("\uFEFF" + ProxyList, out var skipped);

        Assert.AreEqual(2, servers.Count);
        Assert.AreEqual(3, skipped);
        var ss = (ShadowsocksServer)servers[0];
        Assert.AreEqual("香港, 01", ss.Remarks);
        Assert.AreEqual("00: #pass", ss.Password);
        Assert.AreEqual("obfs-local", ss.ProtoExtra.SsPlugin);
        Assert.AreEqual("obfs=tls;obfs-host=cdn.example", ss.ProtoExtra.SsPluginOptions);
        var shared = (ShadowsocksServer)ShareLink.ParseText(ShareLink.GetShareLink(ss)).Single();
        Assert.AreEqual(ss.ProtoExtra.SsPluginOptions, shared.ProtoExtra.SsPluginOptions);
        var vless = (VLESSServer)servers[1];
        Assert.AreEqual("tls.example", vless.Sni);
        Assert.AreEqual("ws.example", vless.RequestHost);
        Assert.AreEqual("/ws?ed=2048", vless.Path);
        Assert.AreEqual("h2,http/1.1", vless.Alpn);
        Assert.AreEqual("chrome", vless.Fingerprint);
        Assert.AreEqual(false, vless.AllowInsecure);
        Assert.AreEqual(false, vless.MuxEnabled);
    }

    [TestMethod]
    public void AnyTlsImportPreservesIdleSessionSettings()
    {
        const string yaml = """
            proxies:
              - {name: Tuned, type: anytls, server: tls.example, port: 443, password: test, idle-session-check-interval: 30, idle-session-timeout: 45, min-idle-session: 0}
              - {name: Default, type: anytls, server: tls.example, port: 443, password: test}
              - {name: Invalid, type: anytls, server: tls.example, port: 443, password: test, idle-session-timeout: -1}
            """;

        var servers = ShareLink.ParseText(yaml, out var skipped);
        Assert.AreEqual(2, servers.Count);
        Assert.AreEqual(1, skipped);

        var options = Netch.Global.NewCustomJsonSerializerOptions();
        options.Converters.Add(new ServerConverterWithTypeDiscriminator());
        var saved = (AnyTLSServer)JsonSerializer.Deserialize<Server>(JsonSerializer.Serialize(servers[0], options), options);
        Assert.AreEqual(30, saved.ProtoExtra.AnyTlsIdleSessionCheckIntervalSeconds);
        Assert.AreEqual(45, saved.ProtoExtra.AnyTlsIdleSessionTimeoutSeconds);
        Assert.AreEqual(0, saved.ProtoExtra.AnyTlsMinIdleSession);
        Assert.IsNull(servers[1].ProtoExtra.AnyTlsIdleSessionCheckIntervalSeconds);
        Assert.IsNull(servers[1].ProtoExtra.AnyTlsIdleSessionTimeoutSeconds);
        Assert.IsNull(servers[1].ProtoExtra.AnyTlsMinIdleSession);
    }

    [TestMethod]
    public void ShadowsocksImportPreservesTheSupportedUotVersion()
    {
        const string yaml = """
            proxies:
              - {name: Default, type: ss, server: ss.example, port: 443, cipher: aes-128-gcm, password: test, udp-over-tcp: true}
              - {name: Version 2, type: ss, server: ss.example, port: 443, cipher: aes-256-cfb, password: test, udp-over-tcp: true, udp-over-tcp-version: 2}
              - {name: Unsupported path, type: ss, server: ss.example, port: 443, cipher: aes-128-gcm, password: test, plugin: v2ray-plugin, plugin-opts: {path: /a;b}}
            """;

        var server = ShareLink.ParseText(yaml, out var skipped).Single();
        Assert.AreEqual(2, skipped);
        Assert.AreEqual("Version 2", server.Remarks);
        Assert.AreEqual(true, server.ProtoExtra.Uot);
        Assert.IsTrue(new ShadowsocksUtil().CheckServer(server));
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void KeepsShareLinkSubscriptionFormats(bool base64)
    {
        const string links = """
            socks5://user:pass@socks.example:1080#Existing proxies: HK
            socks5://user:pass@socks2.example:1080#Existing
            """;
        var servers = ShareLink.ParseText(base64 ? ShareLink.URLSafeBase64Encode(links) : links);
        CollectionAssert.AreEqual(new[] { "socks.example", "socks2.example" }, servers.Select(server => server.Address).ToArray());
    }

    [TestMethod]
    [DataRow("", WebUtil.DefaultUserAgent)]
    [DataRow("mihomo", "mihomo")]
    public async Task SubscriptionRefreshRetainsIdsAndKeepsNodesOnFailureAsync(string userAgent, string expectedAgent)
    {
        using var port = new TcpListener(IPAddress.Loopback, 0);
        port.Start();
        var url = $"http://127.0.0.1:{((IPEndPoint)port.LocalEndpoint).Port}/";
        port.Stop();
        using var listener = new HttpListener();
        listener.Prefixes.Add(url);
        listener.Start();
        var subscription = new Subscription { Remark = Guid.NewGuid().ToString(), Link = url, UserAgent = userAgent };
        var old = new ShadowsocksServer { Address = "ss.example", Port = 443, Group = subscription.Remark };
        var settings = Netch.Global.Settings;
        settings.Server.Add(old);
        settings.Subscription.Add(subscription);
        try
        {
            async Task<int> RefreshAsync(string body)
            {
                var update = SubscriptionUtil.UpdateServerAsync(subscription);
                var context = await listener.GetContextAsync().WaitAsync(TimeSpan.FromSeconds(5));
                var requestAgent = context.Request.UserAgent;
                context.Response.AddHeader("subscription-userinfo", "upload=10; download=20; total=100");
                var payload = Encoding.UTF8.GetBytes(body);
                context.Response.ContentLength64 = payload.Length;
                await context.Response.OutputStream.WriteAsync(payload);
                context.Response.Close();
                var count = await update.WaitAsync(TimeSpan.FromSeconds(5));
                Assert.AreEqual(expectedAgent, requestAgent);
                return count;
            }

            Assert.AreEqual(2, await RefreshAsync(ProxyList));
            var imported = settings.Server.Where(node => node.Group == subscription.Remark).ToArray();
            Assert.AreEqual(old.Id, imported[0].Id);
            Assert.IsTrue(imported.All(node => node.IsSub));
            Assert.AreEqual(70UL, subscription.TrafficRemaining);

            Assert.AreEqual(0, await RefreshAsync("proxies: [{type: snell}]"));
            Assert.IsTrue(subscription.UpdateFailed);
            CollectionAssert.AreEqual(imported, settings.Server.Where(node => node.Group == subscription.Remark).ToArray());
        }
        finally
        {
            settings.Subscription.Remove(subscription);
            settings.Server.RemoveAll(node => node.Group == subscription.Remark);
        }
    }

    [TestMethod]
    public void NodeGroupHelperSeparatesImportsAndSubscriptionsAndRenamesSafely()
    {
        var subscriptions = new[] { new Subscription { Remark = "Airport" } };
        var existing = new Server[]
        {
            new SocksServer("sub.example", 1080) { Group = "Airport", IsSub = true },
            new SocksServer("local.example", 1080) { Group = "MyNodes", IsSub = false }
        };

        Assert.AreEqual("custom-name", NodeGroupHelper.ResolveImportGroupName(ProxyList, @"C:\Downloads\custom-name.yaml", subscriptions, existing));
        Assert.AreEqual("nodes", NodeGroupHelper.ResolveImportGroupName("ss://...", @"C:\Downloads\nodes.txt", subscriptions, existing));
        Assert.AreEqual("airport (2)", NodeGroupHelper.ResolveImportGroupName(ProxyList, @"C:\Downloads\airport.yml", subscriptions, existing));
        Assert.AreEqual("mynodes (2)", NodeGroupHelper.ResolveImportGroupName(ProxyList, @"C:\Downloads\mynodes.yaml", subscriptions, existing));
        Assert.AreEqual("Clash YAML", NodeGroupHelper.ResolveImportGroupName(ProxyList, null, subscriptions, existing));
        Assert.AreEqual(Netch.Constants.DefaultGroup, NodeGroupHelper.ResolveImportGroupName("socks5://127.0.0.1:1080#S", null, subscriptions, existing));

        Assert.IsNotNull(NodeGroupHelper.ValidateGroupName("", null, subscriptions, existing));
        Assert.IsNotNull(NodeGroupHelper.ValidateGroupName("none", null, subscriptions, existing));
        Assert.IsNotNull(NodeGroupHelper.ValidateGroupName("airport", "MyNodes", subscriptions, existing));
        Assert.IsNull(NodeGroupHelper.ValidateGroupName("mynodes", "MyNodes", subscriptions, existing));

        var importedNode = existing[1];
        var originalId = importedNode.Id;
        var scenes = new[] { new Scene { Name = "S", ServerId = originalId } };

        Assert.IsTrue(NodeGroupHelper.RenameGroup("MyNodes", "RenamedLocal", isSubscription: false, out var error, subscriptions, existing));
        Assert.IsNull(error);
        Assert.AreEqual("RenamedLocal", importedNode.Group);
        Assert.AreEqual(originalId, importedNode.Id);
        Assert.AreEqual("Airport", existing[0].Group);
        Assert.AreEqual(1, NodeGroupHelper.CountAffectedScenes([importedNode], scenes));

        Assert.IsTrue(NodeGroupHelper.RenameGroup("Airport", "AirportNew", isSubscription: true, out error, subscriptions, existing));
        Assert.IsNull(error);
        Assert.AreEqual("AirportNew", subscriptions[0].Remark);
        Assert.AreEqual("AirportNew", existing[0].Group);
        Assert.AreEqual("RenamedLocal", importedNode.Group);
        CollectionAssert.AreEqual(new[] { "RenamedLocal" }, NodeGroupHelper.GetImportedGroups(existing).ToArray());
    }

    [TestMethod]
    public async Task SubscriptionRefreshDoesNotDeleteSameNamedLocalImportedNodesAsync()
    {
        using var port = new TcpListener(IPAddress.Loopback, 0);
        port.Start();
        var url = $"http://127.0.0.1:{((IPEndPoint)port.LocalEndpoint).Port}/";
        port.Stop();
        using var listener = new HttpListener();
        listener.Prefixes.Add(url);
        listener.Start();

        var remark = "Shared-" + Guid.NewGuid().ToString("N")[..6];
        var subscription = new Subscription { Remark = remark, Link = url };
        var localImported = new SocksServer("local-keep.example", 1080) { Group = remark, IsSub = false };
        var settings = Netch.Global.Settings;
        settings.Server.Add(localImported);
        settings.Subscription.Add(subscription);
        try
        {
            var update = SubscriptionUtil.UpdateServerAsync(subscription);
            var context = await listener.GetContextAsync().WaitAsync(TimeSpan.FromSeconds(5));
            var payload = Encoding.UTF8.GetBytes("socks5://sub.example:1080#Sub");
            context.Response.ContentLength64 = payload.Length;
            await context.Response.OutputStream.WriteAsync(payload);
            context.Response.Close();

            Assert.AreEqual(1, await update.WaitAsync(TimeSpan.FromSeconds(5)));
            Assert.IsTrue(settings.Server.Contains(localImported));
            Assert.IsTrue(settings.Server.Any(s => s.IsSub && s.Group == remark && s.Address == "sub.example"));
        }
        finally
        {
            settings.Subscription.Remove(subscription);
            settings.Server.RemoveAll(node => node.Group == remark);
        }
    }

    [TestMethod]
    public void ClashImportParsesShadowTlsSshAndHysteria2IncrementalOptions()
    {
        var yaml = """
            proxies:
              - name: stls-clash
                type: ss
                server: stls.example
                port: 443
                cipher: aes-128-gcm
                password: ss-pw
                udp-over-tcp: true
                plugin: shadow-tls
                plugin-opts:
                  host: sni.example
                  password: stls-pw
                  version: 3
                  alpn:
                    - h2
              - name: ssh-clash
                type: ssh
                server: ssh.example
                username: ops
                password: pw
              - name: hy2-clash
                type: hysteria2
                server: hy2.example
                port: 443
                password: hy2pw
                ports: 10000-20000
                hop-interval: 15
                hop-interval-max: 30
                bbr-profile: aggressive
            """;

        var servers = ShareLink.ParseText(yaml, out var skipped);
        Assert.AreEqual(0, skipped);
        Assert.AreEqual(3, servers.Count);

        var stls = (ShadowTLSServer)servers[0];
        Assert.AreEqual("aes-128-gcm", stls.ProtoExtra.SsMethod);
        Assert.AreEqual(3, stls.ProtoExtra.ShadowTlsVersion);
        Assert.AreEqual("stls-pw", stls.ProtoExtra.ShadowTlsPassword);
        Assert.AreEqual("sni.example", stls.Sni);
        Assert.AreEqual("h2", stls.Alpn);
        Assert.AreEqual(true, stls.ProtoExtra.Uot);

        var ssh = (SSHServer)servers[1];
        Assert.AreEqual(22, ssh.Port);
        Assert.AreEqual("ops", ssh.Username);

        var hy2 = (Hysteria2Server)servers[2];
        Assert.AreEqual("15", hy2.ProtoExtra.HopInterval);
        Assert.AreEqual("30", hy2.ProtoExtra.HopIntervalMax);
        Assert.AreEqual("aggressive", hy2.ProtoExtra.BbrProfile);
    }

    [TestMethod]
    public void ClashImportHandlesShadowTlsUotVersionAndSshAliases()
    {
        var yaml = """
            proxies:
              - name: stls-uot2
                type: ss
                server: stls2.example
                port: 443
                cipher: aes-128-gcm
                password: ss-pw
                udp-over-tcp: true
                udp-over-tcp-version: 2
                plugin: shadow-tls
                plugin-opts:
                  host: sni.example
                  password: stls-pw
                  version: 3
                  skip-cert-verify: true
                  client-fingerprint: firefox
              - name: stls-uot1-rejected
                type: ss
                server: stls1.example
                port: 443
                cipher: aes-128-gcm
                password: ss-pw
                udp-over-tcp: true
                udp-over-tcp-version: 1
                plugin: shadow-tls
                plugin-opts:
                  host: sni.example
                  password: stls-pw
                  version: 3
              - name: ssh-alias
                type: ssh
                server: ssh.example
                username: ops
                private-key: |
                  -----BEGIN OPENSSH PRIVATE KEY-----
                  dGVzdA==
                  -----END OPENSSH PRIVATE KEY-----
                private_key_passphrase: key-pass
                host-key: ssh-ed25519 AAAAC3NzaC1lZDI1NTE5AAAAIAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8=
            """;

        var servers = ShareLink.ParseText(yaml, out var skipped);
        Assert.AreEqual(1, skipped);
        Assert.AreEqual(2, servers.Count);

        var stls = (ShadowTLSServer)servers[0];
        Assert.AreEqual(true, stls.ProtoExtra.Uot);
        Assert.AreEqual(true, stls.AllowInsecure);
        Assert.AreEqual("firefox", stls.Fingerprint);

        var ssh = (SSHServer)servers[1];
        Assert.AreEqual("key-pass", ssh.ProtoExtra.SshPrivateKeyPassphrase);
        StringAssert.Contains(ssh.ProtoExtra.SshHostKey, "ssh-ed25519 ");
    }

    [TestMethod]
    public void ClashImportParsesTrustTunnelMieruShadowQuicAndAmneziaWgAndRejectsInvalidVariants()
    {
        var wgKey = Convert.ToBase64String(Enumerable.Range(1, 32).Select(n => (byte)n).ToArray());
        var patternB64 = Convert.ToBase64String([10, 20, 30, 40]);

        var yaml = $"""
            proxies:
              - name: tt-quic
                type: trusttunnel
                server: tt.example
                port: 443
                username: tt-u
                password: tt-p
                udp: true
                sni: edge.example
                alpn: [h3, h2]
                client-fingerprint: chrome
                name-cert-verify: cert.example
                health-check: true
                quic: true
                congestion-controller: BBR
                bbr-profile: CONSERVATIVE
                max-connections: 8
                min-streams: 4
                ech-opts:
                  enable: true
                  config: AQIDBA==
              - name: tt-conflict-rejected
                type: trusttunnel
                server: tt-bad.example
                port: 443
                username: u
                password: p
                max-connections: 8
                max-streams: 16
              - name: mieru-range
                type: mieru
                server: mieru.example
                port-range: 2010-2020
                transport: udp
                username: m-u
                password: m-p
                udp: true
                multiplexing: MULTIPLEXING_MIDDLE
                handshake-mode: HANDSHAKE_NO_WAIT
                traffic-pattern: {patternB64}
              - name: mieru-both-ports-rejected
                type: mieru
                server: mieru-bad.example
                port: 2010
                port-range: 2010-2020
                transport: TCP
                username: m-u
                password: m-p
              - name: mieru-zero-port-range-rejected
                type: mieru
                server: mieru-bad3.example
                port: 0
                port-range: 2010-2020
                transport: TCP
                username: m-u
                password: m-p
              - name: mieru-bad-b64-rejected
                type: mieru
                server: mieru-bad2.example
                port: 2010
                transport: TCP
                username: m-u
                password: m-p
                traffic-pattern: not-valid-base64!!!
              - name: sq-node
                type: shadowquic
                server: sq.example
                port: 10443
                username: sq-u
                password: sq-p
                sni: cam.example
                alpn: h3
                quic-versions: [v1, v2]
                udp-over-stream: true
                zero-rtt: true
                keep-alive-interval: 10000
                congestion-controller: brutal
                up: 100 Mbps
                down: 200 Mbps
                cwnd: 32
                max-datagram-frame-size: 1400
                max-open-streams: 1024
                recv-window-conn: 65536
                recv-window: 32768
                disable-mtu-discovery: true
              - name: sq-bad-version-rejected
                type: shadowquic
                server: sq-bad.example
                port: 10443
                username: sq-u
                password: sq-p
                quic-versions: [v9]
              - name: wg-plain
                type: wireguard
                server: wg.example
                port: 51820
                ip: 10.0.0.2/32
                private-key: {wgKey}
                public-key: {wgKey}
              - name: awg-v3
                type: wireguard
                server: awg.example
                port: 51820
                ip: 10.0.0.2/32
                ipv6: fd00::2/128
                private-key: {wgKey}
                public-key: {wgKey}
                mtu: 1380
                amnezia-wg-option:
                  version: 3
                  jc: 4
                  jmin: 40
                  jmax: 70
                  s1: 15
                  s2: 20
                  s3: 18
                  s4: 12
                  h1: 100-200
                  h2: 2
                  h3: 3
                  h4: 4
                  i1: "<b 0xc7>"
                  header-protection-key: {wgKey}
                  content-padding-addition: 10-50
                  random-trailers: true
                  disable-cookies: true
            """;

        var servers = ShareLink.ParseText(yaml, out var skipped);
        Assert.AreEqual(5, skipped);
        Assert.AreEqual(5, servers.Count);

        var tt = (TrustTunnelServer)servers[0];
        Assert.AreEqual("edge.example", tt.Sni);
        Assert.AreEqual("h3,h2", tt.Alpn);
        Assert.AreEqual(true, tt.ProtoExtra.TrustTunnelQuic);
        Assert.AreEqual("bbr", tt.ProtoExtra.CongestionControl);
        Assert.AreEqual("conservative", tt.ProtoExtra.BbrProfile);
        Assert.AreEqual("AQIDBA==", tt.EchConfigList);

        var mieru = (MieruServer)servers[1];
        Assert.AreEqual(2010, mieru.Port);
        Assert.AreEqual("2010-2020", mieru.ProtoExtra.MieruPortRange);
        Assert.AreEqual("UDP", mieru.ProtoExtra.MieruTransport);
        Assert.AreEqual("MULTIPLEXING_MIDDLE", mieru.ProtoExtra.MieruMultiplexing);
        Assert.AreEqual("HANDSHAKE_NO_WAIT", mieru.ProtoExtra.MieruHandshakeMode);
        Assert.AreEqual(patternB64, mieru.ProtoExtra.MieruTrafficPattern);

        var sq = (ShadowQUICServer)servers[2];
        Assert.AreEqual("v1,v2", sq.ProtoExtra.ShadowQuicVersions);
        Assert.AreEqual("brutal", sq.ProtoExtra.CongestionControl);
        Assert.AreEqual(100, sq.ProtoExtra.UpMbps);
        Assert.AreEqual(200, sq.ProtoExtra.DownMbps);
        Assert.AreEqual(true, sq.ProtoExtra.ShadowQuicDisableMtuDiscovery);

        Assert.IsInstanceOfType<WireGuardServer>(servers[3]);
        Assert.IsNotInstanceOfType<AmneziaWGServer>(servers[3]);

        var awg = (AmneziaWGServer)servers[4];
        Assert.AreEqual(3, awg.ProtoExtra.AwgVersion);
        Assert.AreEqual("100-200", awg.ProtoExtra.AwgH1);
        Assert.AreEqual("<b 0xc7>", awg.ProtoExtra.AwgI1);
        Assert.AreEqual(wgKey, awg.ProtoExtra.AwgHeaderProtectionKey);
        Assert.AreEqual(true, awg.ProtoExtra.AwgRandomTrailers);
    }
}
