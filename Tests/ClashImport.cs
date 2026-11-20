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
}
