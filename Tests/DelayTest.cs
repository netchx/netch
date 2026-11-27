#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Netch.Enums;
using Netch.Models;
using Netch.Servers;
using Netch.Services;
using Netch.Utils;

namespace Tests;

[TestClass]
public class DelayTest
{
    private const string Uuid = "ac7f559b-d6d0-4bdc-96b1-5e0bd260779f";
    private static readonly string WgKey = Convert.ToBase64String(Enumerable.Range(1, 32).Select(n => (byte)n).ToArray());

    private Setting _savedSettings = null!;

    [TestInitialize]
    public void SaveSettings()
    {
        _savedSettings = Netch.Global.Settings;
        Netch.Global.Settings = new Setting();
    }

    [TestCleanup]
    public void RestoreSettings()
    {
        DelayTestHelper.Cancel();
        Netch.Global.Settings = _savedSettings;
    }

    [TestMethod]
    public void SupportsTcpPreScreenClassifiesAllProtocols()
    {
        Server[] tcpServers =
        [
            new SocksServer("1.2.3.4", 1080),
            new HttpServer("1.2.3.4", 443) { StreamSecurity = "tls" },
            new ShadowsocksServer { Address = "1.2.3.4", Port = 443, Password = "pw", ProtoExtra = { SsMethod = "aes-128-gcm" } },
            new ShadowsocksRServer { Address = "1.2.3.4", Port = 443, Password = "pw", EncryptMethod = "aes-256-cfb", Protocol = "origin", OBFS = "plain" },
            new VMessServer { Address = "1.2.3.4", Port = 443, Password = Uuid, Network = "ws", StreamSecurity = "tls" },
            new VLESSServer { Address = "1.2.3.4", Port = 443, Password = Uuid, Network = "xhttp", StreamSecurity = "reality", PublicKey = "pub" },
            new TrojanServer { Address = "1.2.3.4", Port = 443, Password = "pw", Network = "grpc", StreamSecurity = "tls" },
            new AnyTLSServer { Address = "1.2.3.4", Port = 443, Password = "pw" },
            new NaiveServer { Address = "1.2.3.4", Port = 443, Username = "u", Password = "p", ProtoExtra = { NaiveQuic = false } },
            new ShadowTLSServer { Address = "1.2.3.4", Port = 443, Password = "pw", ProtoExtra = { ShadowTlsPassword = "hpw" } },
            new SSHServer { Address = "1.2.3.4", Port = 22, Username = "root", Password = "pw" },
            new TrustTunnelServer { Address = "1.2.3.4", Port = 443, Username = "u", Password = "p", ProtoExtra = { TrustTunnelQuic = false } },
            new MieruServer { Address = "1.2.3.4", Port = 2010, Username = "u", Password = "p", ProtoExtra = { MieruTransport = "TCP" } }
        ];

        foreach (var server in tcpServers)
            Assert.IsTrue(server.SupportsTcpPreScreen(), $"Expected TCP pre-screen support for {server.ConfigType}/{server.Network}");

        Server[] udpOrQuicServers =
        [
            new Hysteria2Server { Address = "1.2.3.4", Port = 443, Password = "pw" },
            new TUICServer { Address = "1.2.3.4", Port = 443, Username = Uuid, Password = "pw" },
            new WireGuardServer { Address = "1.2.3.4", Port = 51820, Password = WgKey, PublicKey = WgKey, ProtoExtra = { WgInterfaceAddress = "10.0.0.2/32" } },
            new MASQUEServer { Address = "1.2.3.4", Port = 443 },
            new NaiveServer { Address = "1.2.3.4", Port = 443, Username = "u", Password = "p", ProtoExtra = { NaiveQuic = true } },
            new TrustTunnelServer { Address = "1.2.3.4", Port = 443, Username = "u", Password = "p", ProtoExtra = { TrustTunnelQuic = true } },
            new MieruServer { Address = "1.2.3.4", Port = 2010, Username = "u", Password = "p", ProtoExtra = { MieruTransport = "UDP" } },
            new ShadowQUICServer { Address = "1.2.3.4", Port = 443, Username = "u", Password = "p" },
            new AmneziaWGServer { Address = "1.2.3.4", Port = 51820, Password = WgKey, PublicKey = WgKey, ProtoExtra = { WgInterfaceAddress = "10.0.0.2/32" } },
            new ExternalCliServer { Address = "127.0.0.1", Port = 1080, ProtoExtra = { CliExecutable = "cli.exe" } },
            new VMessServer { Address = "1.2.3.4", Port = 443, Password = Uuid, Network = "quic" },
            new VMessServer { Address = "1.2.3.4", Port = 443, Password = Uuid, Network = "kcp" },
            new ShadowsocksServer { Address = "1.2.3.4", Port = 443, Password = "pw", ProtoExtra = { SsPlugin = "v2ray-plugin", SsPluginOptions = "mode=quic;host=example.com" } }
        ];

        foreach (var server in udpOrQuicServers)
            Assert.IsFalse(server.SupportsTcpPreScreen(), $"Expected UDP/QUIC direct core probe for {server.ConfigType}/{server.Network}");
    }

    [TestMethod]
    public async Task TcpPingDistinguishesReachableRefusedAndTimeoutAndInvalidPort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var openPort = ((IPEndPoint)listener.LocalEndpoint).Port;
        var closedPort = AllocateClosedPort();

        var openDelay = await Utils.TCPingAsync(IPAddress.Loopback, openPort, 3000);
        Assert.IsTrue(DelayStatus.IsValid(openDelay), $"Expected >= 0 for open port, got {openDelay}");

        var refusedDelay = await Utils.TCPingAsync(IPAddress.Loopback, closedPort, 3500);
        Assert.AreEqual(DelayStatus.Unreachable, refusedDelay);

        var timeoutDelay = await Utils.TCPingAsync(IPAddress.Loopback, closedPort, 300);
        Assert.AreEqual(DelayStatus.Timeout, timeoutDelay);

        var invalidPortDelay = await Utils.TCPingAsync(IPAddress.Loopback, 0, 1000);
        Assert.AreEqual(DelayStatus.Unreachable, invalidPortDelay);
    }

    [TestMethod]
    public async Task DelayTestHelperCoalescesEndpointsReportsProgressAndRestoresOnCancel()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var openPort = (ushort)((IPEndPoint)listener.LocalEndpoint).Port;
        var closedPort = AllocateClosedPort();

        Netch.Global.Settings.DelayTestMode = DelayTestMode.TCPing;
        Netch.Global.Settings.DelayTestTimeout = 3500;

        var s1 = new SocksServer("127.0.0.1", openPort) { Remarks = "N1" };
        var s2 = new SocksServer("127.0.0.1", openPort) { Remarks = "N2" };
        var s3 = new SocksServer("127.0.0.1", openPort) { Remarks = "N3" };
        var sDead = new SocksServer("127.0.0.1", closedPort) { Remarks = "Dead" };

        var progressEvents = new List<(bool IsTesting, int Completed, int Total)>();
        void OnProgress(bool testing, int done, int total)
        {
            lock (progressEvents)
                progressEvents.Add((testing, done, total));
        }

        AppEvents.DelayTestProgressChanged += OnProgress;
        try
        {
            await DelayTestHelper.PerformTestAsync([s1, s2, s3, sDead], waitFinish: true);
        }
        finally
        {
            AppEvents.DelayTestProgressChanged -= OnProgress;
        }

        Assert.IsTrue(DelayStatus.IsValid(s1.Delay), $"Expected >= 0, got {s1.Delay}");
        Assert.AreEqual(s1.Delay, s2.Delay, "Coalesced endpoint nodes should share the probe result");
        Assert.AreEqual(s1.Delay, s3.Delay, "Coalesced endpoint nodes should share the probe result");
        Assert.AreEqual(DelayStatus.Unreachable, sDead.Delay);
        Assert.IsFalse(DelayTestHelper.IsTesting);
        Assert.IsFalse(DelayTestHelper.LastRunCancelled);
        Assert.AreEqual(3, DelayTestHelper.LastReachableCount);
        Assert.IsTrue(progressEvents.Count >= 2);
        Assert.AreEqual((true, 0, 4), progressEvents[0]);
        Assert.AreEqual((false, 4, 4), progressEvents[^1]);

        // Verify cancellation restores Testing nodes back to their previous delay.
        var previousDelay = s1.Delay;
        using var cts = new CancellationTokenSource();
        void CancelOnStart(bool testing, int done, int total)
        {
            if (testing && done == 0)
                cts.Cancel();
        }

        AppEvents.DelayTestProgressChanged += CancelOnStart;
        try
        {
            await DelayTestHelper.PerformTestAsync([s1], waitFinish: true, ct: cts.Token);
        }
        finally
        {
            AppEvents.DelayTestProgressChanged -= CancelOnStart;
        }

        Assert.IsTrue(DelayTestHelper.LastRunCancelled);
        Assert.AreEqual(previousDelay, s1.Delay);
    }

    [TestMethod]
    public async Task DnsUtilsCachesAndFallsBackToSystemDns()
    {
        DnsUtils.ClearCache();

        var literal = await DnsUtils.LookupAllAsync("127.0.0.1");
        CollectionAssert.AreEqual(new[] { IPAddress.Loopback }, literal);

        var deadDnsPort = AllocateClosedPort();
        var fallback = await DnsUtils.LookupAllAsync("localhost", dns: $"127.0.0.1:{deadDnsPort}", timeout: 500, useCache: false);
        Assert.IsTrue(fallback.Length > 0, "Expected unreachable custom DNS to fall back to system DNS");
        Assert.IsTrue(fallback.Any(ip => IPAddress.IsLoopback(ip)));

        var cached = await DnsUtils.LookupAllAsync("localhost", useCache: true);
        Assert.IsTrue(cached.Length > 0);
    }

    [TestMethod]
    public async Task DnsUtilsSingleFamilyCacheDoesNotPoisonUnspecifiedLookup()
    {
        DnsUtils.ClearCache();
        var v4Baseline = await DnsUtils.LookupAllAsync("localhost", AddressFamily.InterNetwork, useCache: false);
        var v6Baseline = await DnsUtils.LookupAllAsync("localhost", AddressFamily.InterNetworkV6, useCache: false);
        if (v4Baseline.Length == 0 || v6Baseline.Length == 0)
            Assert.Inconclusive("Dual-stack loopback DNS lookup is unavailable in this environment.");

        DnsUtils.ClearCache();
        var v6Only = await DnsUtils.LookupAllAsync("localhost", AddressFamily.InterNetworkV6, useCache: true);
        Assert.IsTrue(v6Only.Length > 0 && v6Only.All(ip => ip.AddressFamily == AddressFamily.InterNetworkV6));

        var unspecified = await DnsUtils.LookupAllAsync("localhost", AddressFamily.Unspecified, useCache: true);
        Assert.IsTrue(unspecified.Any(ip => ip.AddressFamily == AddressFamily.InterNetwork),
            $"Expected IPv4 address in Unspecified lookup after caching IPv6, got [{string.Join(", ", unspecified.Select(i => i.ToString()))}]");
    }

    [TestMethod]
    public void BatchProbeBuildersHonorResolvedAddressAndPreserveHostAndModel()
    {
        var buildProxyTypes = new[] { typeof(Server), typeof(string), typeof(string) };
        var sbWs = (Dictionary<string, object?>)InvokeStatic("Netch.Servers.SingboxController", "BuildProxy", buildProxyTypes,
            new VMessServer
            {
                Address = "proxy.example",
                Port = 443,
                Password = Uuid,
                Network = "ws",
                StreamSecurity = "tls"
            }, "n0", "1.2.3.4");
        Assert.AreEqual("1.2.3.4", sbWs["server"]);
        var sbTls = (Dictionary<string, object?>)sbWs["tls"]!;
        Assert.AreEqual("proxy.example", sbTls["server_name"]);
        var sbWsTransport = (Dictionary<string, object?>)sbWs["transport"]!;
        var sbWsHeaders = (Dictionary<string, string>)sbWsTransport["headers"]!;
        Assert.AreEqual("proxy.example", sbWsHeaders["Host"]);

        var sbUpgrade = (Dictionary<string, object?>)InvokeStatic("Netch.Servers.SingboxController", "BuildProxy", buildProxyTypes,
            new VLESSServer
            {
                Address = "up.example",
                Port = 443,
                Password = Uuid,
                Network = "httpupgrade"
            }, "n1", "5.6.7.8");
        Assert.AreEqual("5.6.7.8", sbUpgrade["server"]);
        Assert.AreEqual("up.example", ((Dictionary<string, object?>)sbUpgrade["transport"]!)["host"]);

        var sbH2 = (Dictionary<string, object?>)InvokeStatic("Netch.Servers.SingboxController", "BuildProxy", buildProxyTypes,
            new TrojanServer
            {
                Address = "h2.example",
                Port = 443,
                Password = "pw",
                Network = "h2",
                StreamSecurity = "tls"
            }, "n2", "9.9.9.9");
        Assert.AreEqual("9.9.9.9", sbH2["server"]);
        var sbH2Hosts = (List<string>)((Dictionary<string, object?>)sbH2["transport"]!)["host"]!;
        CollectionAssert.AreEqual(new[] { "h2.example" }, sbH2Hosts);

        var genOutboundTypes = new[] { typeof(Server), typeof(string), typeof(string) };
        var xhttpTlsNode = new VLESSServer
        {
            Address = "xhttp.example",
            Port = 443,
            Password = Uuid,
            Network = "xhttp",
            StreamSecurity = "tls"
        };
        var xrayXhttpTls = (Outbounds4Ray)InvokeStatic("Netch.Servers.V2rayConfigUtils", "GenerateOutbound", genOutboundTypes,
            xhttpTlsNode, "n0", "1.2.3.4");
        Assert.AreEqual("1.2.3.4", xrayXhttpTls.settings.vnext![0].address);
        Assert.AreEqual("xhttp.example", xrayXhttpTls.streamSettings.tlsSettings!.serverName);
        Assert.AreEqual("xhttp.example", xrayXhttpTls.streamSettings.xhttpSettings!.host);

        var xrayReality = (Outbounds4Ray)InvokeStatic("Netch.Servers.V2rayConfigUtils", "GenerateOutbound", genOutboundTypes,
            new VLESSServer
            {
                Address = "real.example",
                Port = 443,
                Password = Uuid,
                Network = "xhttp",
                StreamSecurity = "reality",
                PublicKey = "pub"
            }, "n1", "2.2.2.2");
        Assert.AreEqual("2.2.2.2", xrayReality.settings.vnext![0].address);
        Assert.AreEqual("real.example", xrayReality.streamSettings.realitySettings!.serverName);

        var vmess = new VMessServer
        {
            Address = "vmess.example",
            Port = 443,
            Password = Uuid,
            StreamSecurity = "tls",
            AllowInsecure = false,
            ProtoExtra = { VmessSecurity = "auto" }
        };
        var xrayVmess = (Outbounds4Ray)InvokeStatic("Netch.Servers.V2rayConfigUtils", "GenerateOutbound", genOutboundTypes,
            vmess, "n2", "3.3.3.3");
        Assert.AreEqual("zero", xrayVmess.settings.vnext![0].users[0].security);
        Assert.AreEqual("auto", vmess.ProtoExtra.VmessSecurity, "GenerateOutbound must not mutate Server model");

        var batchEntries = new List<(string Tag, Server Server, Outbounds4Ray Outbound)> { ("n0", xhttpTlsNode, xrayXhttpTls) };
        var batchCfg = (Netch.Servers.V2rayConfig)InvokeStatic("Netch.Servers.V2rayConfigUtils", "GenerateBatchProbeConfig",
            [typeof(IReadOnlyList<(string, Server, Outbounds4Ray)>), typeof(ushort), typeof(string)],
            batchEntries, (ushort)19090, "sec");
        Assert.AreEqual("block", batchCfg.outbounds[0].tag);
        Assert.AreEqual("n0", batchCfg.outbounds[1].tag);

        var stlsOutbounds = (IReadOnlyList<Dictionary<string, object?>>)InvokeStatic("Netch.Servers.SingboxController", "BuildOutbounds", buildProxyTypes,
            new ShadowTLSServer
            {
                Address = "stls.example",
                Port = 443,
                Password = "ss-pw",
                Sni = string.Empty,
                ProtoExtra = { SsMethod = "aes-128-gcm", ShadowTlsVersion = 3, ShadowTlsPassword = "stls-pw" }
            }, "n0", "9.8.7.6");
        Assert.AreEqual(2, stlsOutbounds.Count);
        Assert.AreEqual("shadowsocks", stlsOutbounds[0]["type"]);
        Assert.AreEqual("n0-shadowtls", stlsOutbounds[0]["detour"]);
        Assert.AreEqual("shadowtls", stlsOutbounds[1]["type"]);
        Assert.AreEqual("9.8.7.6", stlsOutbounds[1]["server"]);
        Assert.AreEqual("stls.example", ((Dictionary<string, object?>)stlsOutbounds[1]["tls"]!)["server_name"]);
    }

    [TestMethod]
    public void ConfigurationMigratesLegacyDelaySettingsOnce()
    {
        var legacyIcmp = new Setting
        {
            DetectionTick = 10,
            ServerTCPing = false,
            DelayTestConfigVersion = null,
            DelayTestTimeout = 10,
            DelayTestUrl = "invalid-url"
        };
        Configuration.NormalizeDelaySettings(legacyIcmp);
        Assert.AreEqual(0, legacyIcmp.DetectionTick);
        Assert.AreEqual(DelayTestMode.ICMPing, legacyIcmp.DelayTestMode);
        Assert.AreEqual(1, legacyIcmp.DelayTestConfigVersion);
        Assert.AreEqual(Netch.Constants.MinDelayTestTimeout, legacyIcmp.DelayTestTimeout);
        Assert.AreEqual(Netch.Constants.DefaultDelayTestUrl, legacyIcmp.DelayTestUrl);

        var migratedCustom = new Setting
        {
            DetectionTick = 30,
            ServerTCPing = true,
            DelayTestMode = DelayTestMode.UrlTest,
            DelayTestConfigVersion = 1,
            DelayTestTimeout = 99999,
            DelayTestUrl = "https://cp.cloudflare.com/generate_204"
        };
        Configuration.NormalizeDelaySettings(migratedCustom);
        Assert.AreEqual(30, migratedCustom.DetectionTick);
        Assert.AreEqual(DelayTestMode.UrlTest, migratedCustom.DelayTestMode);
        Assert.AreEqual(Netch.Constants.MaxDelayTestTimeout, migratedCustom.DelayTestTimeout);
        Assert.AreEqual("https://cp.cloudflare.com/generate_204", migratedCustom.DelayTestUrl);

        var invalidModeLowTick = new Setting
        {
            DelayTestMode = (DelayTestMode)99,
            DetectionTick = 5,
            DelayTestConfigVersion = 1
        };
        Configuration.NormalizeDelaySettings(invalidModeLowTick);
        Assert.AreEqual(DelayTestMode.Hybrid, invalidModeLowTick.DelayTestMode);
        Assert.AreEqual(10, invalidModeLowTick.DetectionTick);

        var highTick = new Setting
        {
            DetectionTick = 999999,
            DelayTestConfigVersion = 1
        };
        Configuration.NormalizeDelaySettings(highTick);
        Assert.AreEqual(86400, highTick.DetectionTick);
    }

    [TestMethod]
    public void BatchProbeConfigsPassCoreSchemaChecks()
    {
        var repoRoot = FindRepoRoot();
        Assert.IsNotNull(repoRoot);

        var utf8NoBom = new UTF8Encoding(false);
        var temp = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"netch-batch-schema-{Guid.NewGuid():N}"));
        try
        {
            var mihomoExe = FindCoreExe(repoRoot, "mihomo.exe");
            if (mihomoExe != null)
            {
                var entries = new List<(string Tag, Server Server, string Upstream)>
                {
                    ("n0", new SocksServer("127.0.0.1", 1080), "127.0.0.1"),
                    ("n1", new VMessServer { Address = "127.0.0.1", Port = 443, Password = Uuid, Network = "ws", StreamSecurity = "tls" }, "127.0.0.1"),
                    ("n2", new Hysteria2Server { Address = "127.0.0.1", Port = 443, Password = "pw", StreamSecurity = "tls" }, "127.0.0.1"),
                    ("n3", new TUICServer { Address = "127.0.0.1", Port = 443, Username = Uuid, Password = "pw" }, "127.0.0.1"),
                    ("n4", new AnyTLSServer { Address = "127.0.0.1", Port = 443, Password = "pw" }, "127.0.0.1"),
                    ("n5", new ShadowTLSServer { Address = "127.0.0.1", Port = 443, Password = "pw", Sni = "stls.example", ProtoExtra = { SsMethod = "aes-128-gcm", ShadowTlsVersion = 3, ShadowTlsPassword = "hpw" } }, "127.0.0.1"),
                    ("n6", new SSHServer { Address = "127.0.0.1", Port = 22, Username = "root", Password = "pw" }, "127.0.0.1")
                };
                var cfg = InvokeStatic("Netch.Servers.MihomoConfig", "BuildBatchProbeConfig",
                    [typeof(IReadOnlyList<(string, Server, string)>), typeof(ushort), typeof(string)],
                    entries, (ushort)19081, "secret123");
                var path = Path.Combine(temp.FullName, "mihomo-batch.json");
                File.WriteAllText(path, JsonSerializer.Serialize(cfg, Netch.Global.NewCustomJsonSerializerOptions()), utf8NoBom);
                RunSchemaCheck(mihomoExe, $"-t -d \"{temp.FullName}\" -f \"{path}\"", "mihomo batch");
            }

            var singboxExe = FindCoreExe(repoRoot, "sing-box.exe");
            if (singboxExe != null)
            {
                var entries = new List<(string Tag, Server Server)>
                {
                    ("n0", new SocksServer("127.0.0.1", 1080) { ProtoExtra = { Uot = true } }),
                    ("n1", new NaiveServer { Address = "127.0.0.1", Port = 443, Username = "u", Password = "p", ProtoExtra = { NaiveQuic = true } }),
                    ("n2", new WireGuardServer { Address = "127.0.0.1", Port = 51820, Password = WgKey, PublicKey = WgKey, ProtoExtra = { WgInterfaceAddress = "10.0.0.2/32" } }),
                    ("n3", new ShadowTLSServer { Address = "127.0.0.1", Port = 443, Password = "pw", Sni = "v1.example", ProtoExtra = { SsMethod = "aes-128-gcm", ShadowTlsVersion = 1 } }),
                    ("n4", new ShadowTLSServer { Address = "127.0.0.1", Port = 444, Password = "pw", Sni = "v3.example", ProtoExtra = { SsMethod = "aes-128-gcm", ShadowTlsVersion = 3, ShadowTlsPassword = "hpw" } }),
                    ("n5", new SSHServer { Address = "127.0.0.1", Port = 22, Username = "root", Password = "pw", ProtoExtra = { SshClientVersion = "SSH-2.0-OpenSSH_9.6" } })
                };
                var cfg = InvokeStatic("Netch.Servers.SingboxController", "BuildBatchProbeConfig",
                    [typeof(IReadOnlyList<(string, Server)>), typeof(ushort), typeof(string)],
                    entries, (ushort)19082, "secret123");
                var path = Path.Combine(temp.FullName, "singbox-batch.json");
                File.WriteAllText(path, JsonSerializer.Serialize(cfg, Netch.Global.NewCustomJsonSerializerOptions()), utf8NoBom);
                RunSchemaCheck(singboxExe, $"check -c \"{path}\"", "sing-box batch");
            }

            var xrayExe = FindCoreExe(repoRoot, "xray.exe");
            if (xrayExe != null)
            {
                var entries = new List<(string Tag, Server Server)>
                {
                    ("n0", new VLESSServer { Address = "127.0.0.1", Port = 443, Password = Uuid, Network = "xhttp", HeaderType = "packet-up", StreamSecurity = "tls" }),
                    ("n1", new VLESSServer { Address = "127.0.0.1", Port = 443, Password = Uuid, Network = "xhttp", StreamSecurity = "reality", PublicKey = "6AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA", ShortId = "ab" })
                };
                var cfg = InvokeStatic("Netch.Servers.V2rayConfigUtils", "GenerateBatchProbeConfig",
                    [typeof(IReadOnlyList<(string, Server)>), typeof(ushort), typeof(string)],
                    entries, (ushort)19083, "secret123");
                var path = Path.Combine(temp.FullName, "xray-batch.json");
                File.WriteAllText(path, JsonSerializer.Serialize(cfg, Netch.Global.NewCustomJsonSerializerOptions()), utf8NoBom);
                RunSchemaCheck(xrayExe, $"run -test -c \"{path}\"", "xray batch");
            }
        }
        finally
        {
            temp.Delete(recursive: true);
        }
    }

    [TestMethod]
    public async Task LiveLoopbackCoreBatchProbeHandlesKeepAliveFailureAndTimeout()
    {
        var repoRoot = FindRepoRoot();
        Assert.IsNotNull(repoRoot);

        var mihomoExe = FindCoreExe(repoRoot, "mihomo.exe");
        var singboxExe = FindCoreExe(repoRoot, "sing-box.exe");
        var xrayExe = FindCoreExe(repoRoot, "xray.exe");
        if (mihomoExe == null || singboxExe == null || xrayExe == null)
            Assert.Inconclusive("Bundled mihomo.exe, sing-box.exe, or xray.exe not found in repository.");

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(35));
        await using var httpResponder = new LoopbackHttp204Server();
        await using var socksBackend = new LoopbackSocks5Server();

        var aliveNode = new SocksServer("127.0.0.1", socksBackend.Port) { Remarks = "Alive" };
        var deadNode = new SocksServer("127.0.0.1", AllocateClosedPort()) { Remarks = "Dead" };
        var validUri = new Uri($"http://127.0.0.1:{httpResponder.Port}/generate_204");
        var hangingUri = new Uri($"http://127.0.0.1:{httpResponder.Port}/hang");

        // 1. Live Mihomo multi-node IN-USER batch probe
        await VerifyCoreBatchProbeAsync(
            coreExe: mihomoExe,
            coreKind: "mihomo",
            aliveNode: aliveNode,
            deadNode: deadNode,
            validUri: validUri,
            hangingUri: hangingUri,
            ct: cts.Token);

        // 2. Live sing-box multi-node auth_user batch probe
        await VerifyCoreBatchProbeAsync(
            coreExe: singboxExe,
            coreKind: "sing-box",
            aliveNode: aliveNode,
            deadNode: deadNode,
            validUri: validUri,
            hangingUri: hangingUri,
            ct: cts.Token);

        // 3. Live xray multi-node user batch probe
        await VerifyCoreBatchProbeAsync(
            coreExe: xrayExe,
            coreKind: "xray",
            aliveNode: aliveNode,
            deadNode: deadNode,
            validUri: validUri,
            hangingUri: hangingUri,
            ct: cts.Token);
    }

    [TestMethod]
    public async Task ProbeOverSocks5SteadyStatePreservesFirstRttWhenBodyDrainTimesOut()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await using var httpResponder = new LoopbackHttp204Server();
        await using var socksBackend = new LoopbackSocks5Server();

        var stallUri = new Uri($"http://127.0.0.1:{httpResponder.Port}/stall-body");
        var rtt = await CoreDelayTester.ProbeOverSocks5Async(
            "127.0.0.1",
            socksBackend.Port,
            username: null,
            password: null,
            stallUri,
            timeoutMs: 600,
            steadySecondRequest: true,
            cts.Token);

        Assert.IsTrue(DelayStatus.IsValid(rtt), $"Expected valid RTT when response headers arrive before body stalls, got {rtt}");
    }

    [TestMethod]
    public async Task CoreDelayTesterBisectsPoisonPillNodeInBatch()
    {
        var repoRoot = FindRepoRoot();
        Assert.IsNotNull(repoRoot);

        var singboxExe = FindCoreExe(repoRoot, "sing-box.exe");
        if (singboxExe == null)
            Assert.Inconclusive("Bundled sing-box.exe not found in repository.");

        var prop = typeof(CoreDelayTester).GetProperty("CoreExecutableResolver", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("CoreExecutableResolver property not found");
        var coreKindType = typeof(CoreSelector).GetNestedType("CoreKind", BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("CoreSelector.CoreKind not found");
        var testBatch = typeof(CoreDelayTester).GetMethod("TestBatchAsync", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("CoreDelayTester.TestBatchAsync not found");

        var param = Expression.Parameter(coreKindType, "kind");
        Func<int, string> resolveByInt = kind => kind == 1 ? singboxExe : string.Empty;
        var resolverDelegate = Expression.Lambda(
            prop.PropertyType,
            Expression.Invoke(Expression.Constant(resolveByInt), Expression.Convert(param, typeof(int))),
            param).Compile();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(25));
        await using var httpResponder = new LoopbackHttp204Server();
        await using var socksBackend = new LoopbackSocks5Server();

        var aliveUot = new SocksServer("127.0.0.1", socksBackend.Port)
        {
            ProtoExtra = { Uot = true },
            Remarks = "HealthyUot"
        };
        var poisonMasque = new MASQUEServer
        {
            Address = "127.0.0.1",
            Port = 443,
            Remarks = "PoisonMasque"
        };

        var results = new ConcurrentDictionary<Server, int>();
        using var sem = new SemaphoreSlim(4);
        var targets = new List<(Server Server, IPAddress PrimaryIp)>
        {
            (aliveUot, IPAddress.Loopback),
            (poisonMasque, IPAddress.Loopback)
        };
        var validUri = new Uri($"http://127.0.0.1:{httpResponder.Port}/generate_204");

        prop.SetValue(null, resolverDelegate);
        try
        {
            var task = (Task)testBatch.Invoke(null, [
                targets,
                validUri,
                2000,
                false,
                sem,
                new Action<Server, int>((s, d) => results[s] = d),
                cts.Token,
                null!
            ])!;
            await task;
        }
        finally
        {
            prop.SetValue(null, null);
        }

        Assert.IsTrue(results.TryGetValue(aliveUot, out var aliveDelay) && DelayStatus.IsValid(aliveDelay),
            $"Expected healthy node in bisected batch to succeed, got {results.GetValueOrDefault(aliveUot, int.MinValue)}");
        Assert.IsTrue(results.TryGetValue(poisonMasque, out var poisonDelay) && poisonDelay == DelayStatus.Unreachable,
            $"Expected unsupported MASQUE poison node to isolate to Unreachable, got {results.GetValueOrDefault(poisonMasque, int.MinValue)}");
    }

    [TestMethod]
    public async Task DelayTestHelperSupersedesPriorManualRunCleanly()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var openPort = (ushort)((IPEndPoint)listener.LocalEndpoint).Port;

        Netch.Global.Settings.DelayTestMode = DelayTestMode.TCPing;
        Netch.Global.Settings.DelayTestTimeout = 1000;

        var s1 = new SocksServer("127.0.0.1", openPort) { Remarks = "S1" };
        var s2 = new SocksServer("127.0.0.1", openPort) { Remarks = "S2" };

        var t1 = DelayTestHelper.PerformTestAsync([s1, s2], waitFinish: true);
        var t2 = DelayTestHelper.PerformTestAsync([s1, s2], waitFinish: true);
        var t3 = DelayTestHelper.PerformTestAsync([s1, s2], waitFinish: true);
        await Task.WhenAll(t1, t2, t3);

        Assert.IsFalse(DelayTestHelper.IsTesting);
        Assert.IsTrue(DelayStatus.IsValid(s1.Delay), $"Expected valid delay for s1, got {s1.Delay}");
        Assert.IsTrue(DelayStatus.IsValid(s2.Delay), $"Expected valid delay for s2, got {s2.Delay}");
    }

    private static async Task VerifyCoreBatchProbeAsync(
        string coreExe,
        string coreKind,
        SocksServer aliveNode,
        SocksServer deadNode,
        Uri validUri,
        Uri hangingUri,
        CancellationToken ct)
    {
        var temp = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"netch-live-probe-{Guid.NewGuid():N}"));
        var port = AllocateClosedPort();
        const string secret = "batchsecret";
        var configPath = Path.Combine(temp.FullName, "probe.json");

        object cfg = coreKind switch
        {
            "mihomo" => InvokeStatic("Netch.Servers.MihomoConfig", "BuildBatchProbeConfig",
                [typeof(IReadOnlyList<(string, Server, string)>), typeof(ushort), typeof(string)],
                new List<(string, Server, string)> { ("n0", aliveNode, "127.0.0.1"), ("n1", deadNode, "127.0.0.1") },
                port, secret),
            "sing-box" => InvokeStatic("Netch.Servers.SingboxController", "BuildBatchProbeConfig",
                [typeof(IReadOnlyList<(string, Server)>), typeof(ushort), typeof(string)],
                new List<(string, Server)> { ("n0", aliveNode), ("n1", deadNode) },
                port, secret),
            "xray" => InvokeStatic("Netch.Servers.V2rayConfigUtils", "GenerateBatchProbeConfig",
                [typeof(IReadOnlyList<(string, Server)>), typeof(ushort), typeof(string)],
                new List<(string, Server)> { ("n0", aliveNode), ("n1", deadNode) },
                port, secret),
            _ => throw new ArgumentOutOfRangeException(nameof(coreKind))
        };

        await File.WriteAllTextAsync(configPath, JsonSerializer.Serialize(cfg, Netch.Global.NewCustomJsonSerializerOptions()), new UTF8Encoding(false), ct);

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = coreExe,
                Arguments = coreKind switch
                {
                    "mihomo" => $"-d \"{temp.FullName}\" -f \"{configPath}\"",
                    "sing-box" => $"run --disable-color -c \"{configPath}\"",
                    "xray" => $"run -c \"{configPath}\"",
                    _ => throw new ArgumentOutOfRangeException(nameof(coreKind))
                },
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        try
        {
            process.Start();
            var coreKindType = typeof(MihomoController).Assembly.GetType("Netch.Servers.CoreSelector+CoreKind")
                ?? throw new InvalidOperationException("CoreSelector.CoreKind not found");
            var coreEnum = Enum.Parse(coreKindType, coreKind switch
            {
                "mihomo" => "Mihomo",
                "sing-box" => "Singbox",
                "xray" => "Xray",
                _ => throw new ArgumentOutOfRangeException(nameof(coreKind))
            });
            var waitTask = (Task)InvokeStatic("Netch.Utils.CoreDelayTester", "WaitForCoreReadyAsync",
                [coreKindType, typeof(Process), typeof(CancellationToken)],
                coreEnum, process, ct);
            await waitTask;
            var startResult = waitTask.GetType().GetProperty("Result")?.GetValue(waitTask)?.ToString();
            Assert.AreEqual("Ready", startResult, $"{coreKind} probe core did not start in time (result: {startResult})");

            var singleRtt = await CoreDelayTester.ProbeOverSocks5Async("127.0.0.1", port, "n0", secret, validUri, 2000, steadySecondRequest: false, ct);
            Assert.IsTrue(DelayStatus.IsValid(singleRtt), $"Expected single-shot valid RTT for {coreKind}, got {singleRtt}");

            var steadyRtt = await CoreDelayTester.ProbeOverSocks5Async("127.0.0.1", port, "n0", secret, validUri, 2000, steadySecondRequest: true, ct);
            Assert.IsTrue(DelayStatus.IsValid(steadyRtt), $"Expected steady-state valid RTT for {coreKind}, got {steadyRtt}");

            var deadRtt = await CoreDelayTester.ProbeOverSocks5Async("127.0.0.1", port, "n1", secret, validUri, 1500, steadySecondRequest: false, ct);
            Assert.IsTrue(DelayStatus.IsFailure(deadRtt), $"Expected failure for dead node in same batch ({coreKind}), got {deadRtt}");

            var timeoutRtt = await CoreDelayTester.ProbeOverSocks5Async("127.0.0.1", port, "n0", secret, hangingUri, 500, steadySecondRequest: false, ct);
            Assert.AreEqual(DelayStatus.Timeout, timeoutRtt, $"Expected Timeout for hanging endpoint ({coreKind}), got {timeoutRtt}");
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
            }
            temp.Delete(recursive: true);
        }
    }

    private static ushort AllocateClosedPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = (ushort)((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static string? FindRepoRoot()
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

    private static string? FindCoreExe(string repoRoot, string name)
    {
        string[] candidates =
        [
            Path.Combine(repoRoot, "Other", "release", name),
            Path.Combine(repoRoot, "Storage", "sing_box", name),
            Path.Combine(repoRoot, "release", "bin", name),
            Path.Combine(repoRoot, "release", "bin", "sing_box", name)
        ];
        return candidates.FirstOrDefault(File.Exists);
    }

    private static object InvokeStatic(string typeName, string methodName, Type[] parameterTypes, params object[] args)
    {
        var type = typeof(MihomoController).Assembly.GetType(typeName)
            ?? throw new InvalidOperationException($"Type {typeName} not found");
        var method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public, null, parameterTypes, null)
            ?? throw new InvalidOperationException($"Method {typeName}.{methodName} not found");
        return method.Invoke(null, args) ?? throw new InvalidOperationException($"{typeName}.{methodName} returned null");
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
        }) ?? throw new InvalidOperationException($"Could not start {label}");
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        Assert.IsTrue(process.WaitForExit(15000), $"{label} timed out");
        Assert.AreEqual(0, process.ExitCode, $"{label} failed ({process.ExitCode}):\n{stdout}\n{stderr}");
    }

    private sealed class LoopbackHttp204Server : IAsyncDisposable
    {
        private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
        private readonly CancellationTokenSource _cts = new();

        public LoopbackHttp204Server()
        {
            _listener.Start();
            Port = (ushort)((IPEndPoint)_listener.LocalEndpoint).Port;
            _ = Task.Run(AcceptAsync);
        }

        public ushort Port { get; }

        private async Task AcceptAsync()
        {
            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    var client = await _listener.AcceptTcpClientAsync(_cts.Token);
                    _ = Task.Run(() => HandleClientAsync(client), _cts.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            using (client)
            {
                var stream = client.GetStream();
                var buffer = new byte[4096];
                while (!_cts.IsCancellationRequested)
                {
                    var total = 0;
                    while (total < buffer.Length)
                    {
                        var read = await stream.ReadAsync(buffer.AsMemory(total, buffer.Length - total), _cts.Token);
                        if (read == 0)
                            return;
                        total += read;
                        var text = Encoding.ASCII.GetString(buffer, 0, total);
                        if (text.Contains("\r\n\r\n", StringComparison.Ordinal))
                        {
                            if (text.StartsWith("GET /hang", StringComparison.Ordinal))
                            {
                                await Task.Delay(TimeSpan.FromSeconds(10), _cts.Token);
                                return;
                            }

                            if (text.StartsWith("GET /stall-body", StringComparison.Ordinal))
                            {
                                var header = "HTTP/1.1 200 OK\r\nContent-Length: 10000\r\nConnection: keep-alive\r\n\r\n"u8.ToArray();
                                await stream.WriteAsync(header, _cts.Token);
                                await stream.FlushAsync(_cts.Token);
                                await Task.Delay(TimeSpan.FromSeconds(10), _cts.Token);
                                return;
                            }

                            var response = "HTTP/1.1 204 No Content\r\nContent-Length: 0\r\nConnection: keep-alive\r\n\r\n"u8.ToArray();
                            await stream.WriteAsync(response, _cts.Token);
                            await stream.FlushAsync(_cts.Token);
                            break;
                        }
                    }
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            await _cts.CancelAsync();
            _listener.Stop();
            _cts.Dispose();
        }
    }

    private sealed class LoopbackSocks5Server : IAsyncDisposable
    {
        private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
        private readonly CancellationTokenSource _cts = new();

        public LoopbackSocks5Server()
        {
            _listener.Start();
            Port = (ushort)((IPEndPoint)_listener.LocalEndpoint).Port;
            _ = Task.Run(AcceptAsync);
        }

        public ushort Port { get; }

        private async Task AcceptAsync()
        {
            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    var client = await _listener.AcceptTcpClientAsync(_cts.Token);
                    _ = Task.Run(() => HandleClientAsync(client), _cts.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            using (client)
            {
                try
                {
                    var stream = client.GetStream();
                    var header = new byte[2];
                    await stream.ReadExactlyAsync(header, _cts.Token);
                    if (header[0] != 5)
                        return;
                    var methods = new byte[header[1]];
                    await stream.ReadExactlyAsync(methods, _cts.Token);
                    await stream.WriteAsync(new byte[] { 5, 0 }, _cts.Token);

                    var req = new byte[4];
                    await stream.ReadExactlyAsync(req, _cts.Token);
                    if (req[0] != 5 || req[1] != 1)
                        return;

                    IPAddress targetIp;
                    switch (req[3])
                    {
                        case 1:
                            var v4 = new byte[4];
                            await stream.ReadExactlyAsync(v4, _cts.Token);
                            targetIp = new IPAddress(v4);
                            break;
                        case 3:
                            var len = new byte[1];
                            await stream.ReadExactlyAsync(len, _cts.Token);
                            var hostBytes = new byte[len[0]];
                            await stream.ReadExactlyAsync(hostBytes, _cts.Token);
                            var host = Encoding.ASCII.GetString(hostBytes);
                            targetIp = (await Dns.GetHostAddressesAsync(host, _cts.Token))[0];
                            break;
                        case 4:
                            var v6 = new byte[16];
                            await stream.ReadExactlyAsync(v6, _cts.Token);
                            targetIp = new IPAddress(v6);
                            break;
                        default:
                            return;
                    }

                    var portBytes = new byte[2];
                    await stream.ReadExactlyAsync(portBytes, _cts.Token);
                    var targetPort = (portBytes[0] << 8) | portBytes[1];

                    using var upstream = new TcpClient(targetIp.AddressFamily);
                    await upstream.ConnectAsync(targetIp, targetPort, _cts.Token);

                    await stream.WriteAsync(new byte[] { 5, 0, 0, 1, 0, 0, 0, 0, 0, 0 }, _cts.Token);
                    var upstreamStream = upstream.GetStream();
                    await Task.WhenAny(
                        stream.CopyToAsync(upstreamStream, _cts.Token),
                        upstreamStream.CopyToAsync(stream, _cts.Token));
                }
                catch
                {
                    // Ignore client disconnect during test teardown.
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            await _cts.CancelAsync();
            _listener.Stop();
            _cts.Dispose();
        }
    }
}
