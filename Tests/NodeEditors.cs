using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Netch.Enums;
using Netch.Interfaces;
using Netch.JsonConverter;
using Netch.Models;
using Netch.Servers;
using Netch.Utils;

namespace Tests;

[TestClass]
public class NodeEditors
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private const string Uuid = "ac7f559b-d6d0-4bdc-96b1-5e0bd260779f";
    private static readonly string Key = Convert.ToBase64String(Enumerable.Range(1, 32).Select(n => (byte)n).ToArray());

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
    public void AllNineteenProtocolsAreRegisteredWithDisplayNamesAndDefaults()
    {
        var protocols = ServerEditorHelper.GetOrderedProtocols();
        CollectionAssert.AreEqual(new[]
        {
            "HTTP", "SOCKS", "Shadowsocks", "ShadowsocksR", "VLESS", "Hysteria2",
            "Trojan", "VMess", "WireGuard", "TUIC", "AnyTLS", "Naive", "MASQUE", "ShadowTLS", "SSH",
            "TrustTunnel", "Mieru", "ShadowQUIC", "AmneziaWG", "ExternalCli"
        }, protocols.Select(util => util.TypeName).ToArray());

        foreach (var util in protocols)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(ServerHelper.GetDisplayName(util)));
            var created = ServerEditorHelper.CreateDefault(util);
            Assert.AreEqual(util.ServerType, created.GetType());
        }

        var vless = ServerEditorHelper.CreateDefault(ServerHelper.GetUtilByTypeName(nameof(EConfigType.VLESS)));
        Assert.AreEqual(false, vless.MuxEnabled);
        Assert.AreEqual(Netch.Constants.DefaultNetwork, vless.Network);
        Assert.AreEqual(Netch.Constants.None, vless.ProtoExtra.VlessEncryption);

        var vmess = ServerEditorHelper.CreateDefault(ServerHelper.GetUtilByTypeName(nameof(EConfigType.VMess)));
        Assert.AreEqual(false, vmess.MuxEnabled);
        Assert.AreEqual(Netch.Constants.DefaultSecurity, vmess.ProtoExtra.VmessSecurity);
        Assert.AreEqual(0, vmess.ProtoExtra.AlterId);

        var hy2 = ServerEditorHelper.CreateDefault(ServerHelper.GetUtilByTypeName(nameof(EConfigType.Hysteria2)));
        Assert.AreEqual(Netch.Constants.StreamSecurity, hy2.StreamSecurity);
        Assert.AreEqual(Netch.Constants.None, hy2.ProtoExtra.Obfs);

        var ss = ServerEditorHelper.CreateDefault(ServerHelper.GetUtilByTypeName(nameof(EConfigType.Shadowsocks)));
        Assert.AreEqual(Netch.Constants.SsCiphers[0], ss.ProtoExtra.SsMethod);

        var stls = ServerEditorHelper.CreateDefault(ServerHelper.GetUtilByTypeName(nameof(EConfigType.ShadowTLS)));
        Assert.AreEqual(Netch.Constants.StreamSecurity, stls.StreamSecurity);
        Assert.AreEqual(3, stls.ProtoExtra.ShadowTlsVersion);
        Assert.AreEqual(Netch.Constants.SsCiphers[0], stls.ProtoExtra.SsMethod);

        var ssh = ServerEditorHelper.CreateDefault(ServerHelper.GetUtilByTypeName(nameof(EConfigType.SSH)));
        Assert.AreEqual(22, ssh.Port);

        var tt = ServerEditorHelper.CreateDefault(ServerHelper.GetUtilByTypeName(nameof(EConfigType.TrustTunnel)));
        Assert.AreEqual(443, tt.Port);
        Assert.AreEqual(Netch.Constants.StreamSecurity, tt.StreamSecurity);
        Assert.AreEqual(true, tt.ProtoExtra.TrustTunnelHealthCheck);

        var mieru = ServerEditorHelper.CreateDefault(ServerHelper.GetUtilByTypeName(nameof(EConfigType.Mieru)));
        Assert.AreEqual("TCP", mieru.ProtoExtra.MieruTransport);
        Assert.AreEqual("MULTIPLEXING_LOW", mieru.ProtoExtra.MieruMultiplexing);
        Assert.AreEqual("HANDSHAKE_STANDARD", mieru.ProtoExtra.MieruHandshakeMode);

        var sq = ServerEditorHelper.CreateDefault(ServerHelper.GetUtilByTypeName(nameof(EConfigType.ShadowQUIC)));
        Assert.AreEqual(443, sq.Port);
        Assert.AreEqual("h3", sq.Alpn);
        Assert.AreEqual("v1", sq.ProtoExtra.ShadowQuicVersions);
        Assert.AreEqual("cubic", sq.ProtoExtra.CongestionControl);

        var awg = ServerEditorHelper.CreateDefault(ServerHelper.GetUtilByTypeName(nameof(EConfigType.AmneziaWG)));
        Assert.AreEqual(1280, awg.ProtoExtra.WgMtu);

        var externalCli = ServerEditorHelper.CreateDefault(ServerHelper.GetUtilByTypeName(nameof(EConfigType.ExternalCli)));
        Assert.AreEqual("127.0.0.1", externalCli.Address);
        Assert.AreEqual(1080, externalCli.Port);

        try
        {
            i18N.Load("zh-CN");
            Assert.AreEqual("添加 VLESS…", i18N.TranslateFormat("Add {0}…", ServerHelper.GetDisplayName(EConfigType.VLESS)));
        }
        finally
        {
            i18N.Load("en-US");
        }
    }

    [TestMethod]
    public void WizardManualProtocolsUseExpectedOrderAndCompleteSet()
    {
        var all = ServerEditorHelper.GetOrderedProtocols();
        var (featured, more) = ServerEditorHelper.GetWizardManualProtocols();

        CollectionAssert.AreEqual(
            new[] { "SOCKS", "Shadowsocks", "ShadowsocksR", "AnyTLS", "VLESS" },
            featured.Select(util => util.TypeName).ToArray());

        CollectionAssert.AreEqual(
            all.Select(util => util.TypeName).Except(["SOCKS", "Shadowsocks", "ShadowsocksR", "AnyTLS", "VLESS"]).ToArray(),
            more.Select(util => util.TypeName).ToArray());

        Assert.AreEqual(all.Count, featured.Count + more.Count);
    }

    [TestMethod]
    public void EditingExistingShadowsocksPluginUsesTheNewPathAndTls()
    {
        var node = new ShadowsocksServer
        {
            Address = "127.0.0.1",
            Port = 443,
            Password = "ui-fixture",
            Network = "ws",
            Path = "/old",
            RequestHost = "old.example",
            StreamSecurity = "tls",
            ProtoExtra = { SsMethod = "aes-128-gcm", SsPlugin = "v2ray-plugin", SsPluginOptions = "mode=websocket;path=/old;tls" }
        };

        node.ProtoExtra.SsPluginOptions = @"mode=websocket;path=/new?a\=1\,2";
        var saved = Save(node, resetShadowsocksTransport: true);

        using var config = JsonDocument.Parse(Build("mihomo", saved));
        var options = config.RootElement.GetProperty("proxies")[0].GetProperty("plugin-opts");
        Assert.AreEqual("/new?a=1,2", options.GetProperty("path").GetString());
        Assert.AreEqual("127.0.0.1", options.GetProperty("host").GetString());
        Assert.IsFalse(options.GetProperty("tls").GetBoolean());
        WriteConfig("Shadowsocks-Edited", config.RootElement.GetRawText());
    }

    [TestMethod]
    public void WireGuardFallbackWritesBareAddressesAsHostPrefixes()
    {
        var server = CreateDefault(EConfigType.WireGuard);
        server.Address = "127.0.0.1";
        server.Port = 51820;
        Populate(server);
        server.ProtoExtra.WgInterfaceAddress = "10.10.10.2,10.10.10.3/24,2001:db8::1";

        using var config = JsonDocument.Parse(Build("sing-box", Save(server)));
        var addresses = config.RootElement.GetProperty("endpoints")[0].GetProperty("address");
        CollectionAssert.AreEqual(new[] { "10.10.10.2/32", "10.10.10.3/24", "2001:db8::1/128" },
            addresses.EnumerateArray().Select(address => address.GetString()).ToArray());
        WriteConfig("WireGuard-Singbox", config.RootElement.GetRawText());
    }

    [TestMethod]
    [DataRow(EConfigType.VLESS, "mihomo", "uuid")]
    [DataRow(EConfigType.VMess, "mihomo", "cipher")]
    [DataRow(EConfigType.Trojan, "mihomo", "password")]
    [DataRow(EConfigType.Shadowsocks, "mihomo", "plugin-opts")]
    [DataRow(EConfigType.ShadowsocksR, "mihomo", "protocol-param")]
    [DataRow(EConfigType.SOCKS, "mihomo", "username")]
    [DataRow(EConfigType.HTTP, "mihomo", "headers")]
    [DataRow(EConfigType.Hysteria2, "mihomo", "hop-interval")]
    [DataRow(EConfigType.TUIC, "mihomo", "congestion-controller")]
    [DataRow(EConfigType.AnyTLS, "mihomo", "sni")]
    [DataRow(EConfigType.WireGuard, "mihomo", "pre-shared-key")]
    [DataRow(EConfigType.Naive, "sing-box", "quic")]
    [DataRow(EConfigType.MASQUE, "sing-box", "headers")]
    [DataRow(EConfigType.ShadowTLS, "mihomo", "plugin-opts")]
    [DataRow(EConfigType.SSH, "mihomo", "private-key")]
    [DataRow(EConfigType.TrustTunnel, "mihomo", "health-check")]
    [DataRow(EConfigType.Mieru, "mihomo", "transport")]
    [DataRow(EConfigType.ShadowQUIC, "mihomo", "quic-versions")]
    [DataRow(EConfigType.AmneziaWG, "mihomo", "amnezia-wg-option")]
    public void EditorSavesParametersUsedByItsCore(EConfigType type, string core, string expectedField)
    {
        var draft = CreateDefault(type);
        draft.Address = "127.0.0.1";
        draft.Port = 443;
        Populate(draft);

        var saved = Save(draft);
        Assert.AreEqual(type, saved.ConfigType);
        if (type == EConfigType.VMess)
        {
            Assert.AreEqual("aes-128-gcm", saved.ProtoExtra.VmessSecurity);
            Assert.AreEqual(0, saved.ProtoExtra.AlterId);
        }
        if (type == EConfigType.VLESS)
        {
            Assert.AreEqual("none", saved.ProtoExtra.VlessEncryption);
            Assert.AreEqual("packet-up", saved.HeaderType);
            Assert.AreEqual("", saved.EchConfigList);
        }
        if (type == EConfigType.WireGuard)
            Assert.AreEqual(Key, new WireGuardUtil().ParseUri(new WireGuardUtil().GetShareLink(saved)).Single().ProtoExtra.WgPresharedKey);

        var jsonOptions = Netch.Global.NewCustomJsonSerializerOptions();
        jsonOptions.Converters.Add(new ServerConverterWithTypeDiscriminator());
        var reopened = JsonSerializer.Deserialize<Server>(JsonSerializer.Serialize(saved, jsonOptions), jsonOptions)!;
        Assert.AreEqual(443, reopened.Port);
        var generated = Build(core, saved);
        Assert.AreEqual(generated, Build(core, Save(reopened)), "Reopening and saving changed the effective configuration.");

        using var doc = JsonDocument.Parse(generated);
        var proxy = core == "mihomo"
            ? doc.RootElement.GetProperty("proxies")[0]
            : doc.RootElement.GetProperty(type == EConfigType.MASQUE ? "endpoints" : "outbounds")[0];
        Assert.IsTrue(proxy.TryGetProperty(expectedField, out _), $"The generated {core} proxy omits {expectedField}.");
        if (type == EConfigType.HTTP)
            Assert.AreEqual("a=1; b=2", proxy.GetProperty("headers").GetProperty("Cookie").GetString());
        if (type == EConfigType.Naive)
            Assert.IsFalse(proxy.GetProperty("tls").TryGetProperty("insecure", out _));
        WriteConfig(type.ToString(), generated);
    }

    [TestMethod]
    public void XhttpEchFieldsSaveSeparatelyAndSocksRejectsPartialAuth()
    {
        var vless = CreateDefault(EConfigType.VLESS);
        vless.Address = "127.0.0.1";
        vless.Port = 443;
        vless.Password = Uuid;
        vless.Network = "xhttp";
        vless.StreamSecurity = "tls";
        vless.Extra = """{"xPaddingBytes":"100-1000"}""";
        var saved = Save(vless);
        var config = V2rayConfigUtils.GenerateClientConfig(saved, "127.0.0.1", 19081);
        WriteConfig("VLESS-Xray", JsonSerializer.Serialize(config, Netch.Global.NewCustomJsonSerializerOptions()));

        saved.EchConfigList = "ech-fixture";
        saved.EchForceQuery = "full";
        saved = Save(saved);
        Assert.AreEqual("ech-fixture", saved.EchConfigList);
        Assert.AreEqual("full", saved.EchForceQuery);
        var tls = V2rayConfigUtils.GenerateClientConfig(saved, "127.0.0.1", 19081).outbounds[0].streamSettings.tlsSettings;
        Assert.AreEqual("ech-fixture", tls.echConfigList);
        Assert.AreEqual("full", tls.echForceQuery);

        var socks = CreateDefault(EConfigType.SOCKS);
        socks.Address = "127.0.0.1";
        socks.Port = 1080;
        socks.Username = "test";
        Assert.IsNotNull(Validate(socks));
    }

    [TestMethod]
    public void EditorsExposeAndSaveAllProtocolSpecificParameters()
    {
        var anyTls = CreateDefault(EConfigType.AnyTLS);
        anyTls.Address = "127.0.0.1";
        anyTls.Port = 443;
        anyTls.Password = "pass";
        anyTls.ProtoExtra.AnyTlsIdleSessionCheckIntervalSeconds = 25;
        anyTls.ProtoExtra.AnyTlsIdleSessionTimeoutSeconds = 60;
        anyTls.ProtoExtra.AnyTlsMinIdleSession = 2;
        using (var doc = JsonDocument.Parse(Build("mihomo", Save(anyTls))))
        {
            var proxy = doc.RootElement.GetProperty("proxies")[0];
            Assert.AreEqual(25, proxy.GetProperty("idle-session-check-interval").GetInt32());
            Assert.AreEqual(60, proxy.GetProperty("idle-session-timeout").GetInt32());
            Assert.AreEqual(2, proxy.GetProperty("min-idle-session").GetInt32());
        }

        var naive = CreateDefault(EConfigType.Naive);
        naive.Address = "127.0.0.1";
        naive.Port = 443;
        naive.Username = "u";
        naive.Password = "p";
        naive.ProtoExtra.NaiveQuic = true;
        naive.ProtoExtra.CongestionControl = "bbr2";
        naive.ProtoExtra.Uot = true;
        using (var doc = JsonDocument.Parse(Build("sing-box", Save(naive))))
        {
            var outbound = doc.RootElement.GetProperty("outbounds")[0];
            Assert.AreEqual("bbr2", outbound.GetProperty("quic_congestion_control").GetString());
            Assert.IsTrue(outbound.GetProperty("udp_over_tcp").GetBoolean());
        }

        var hy2 = CreateDefault(EConfigType.Hysteria2);
        hy2.Address = "127.0.0.1";
        hy2.Port = 443;
        hy2.Password = "p";
        hy2.ProtoExtra.Obfs = "gecko";
        hy2.ProtoExtra.SalamanderPass = "obfs-pw";
        hy2.ProtoExtra.GeckoMinPacketSize = "128";
        hy2.ProtoExtra.GeckoMaxPacketSize = "512";
        var savedHy2 = Save(hy2);
        using (var mihomoDoc = JsonDocument.Parse(Build("mihomo", savedHy2)))
        {
            var mihomoProxy = mihomoDoc.RootElement.GetProperty("proxies")[0];
            Assert.AreEqual("gecko", mihomoProxy.GetProperty("obfs").GetString());
            Assert.AreEqual(128, mihomoProxy.GetProperty("obfs-min-packet-size").GetInt32());
            Assert.AreEqual(512, mihomoProxy.GetProperty("obfs-max-packet-size").GetInt32());
        }
        using (var singDoc = JsonDocument.Parse(Build("sing-box", savedHy2)))
        {
            var singObfs = singDoc.RootElement.GetProperty("outbounds")[0].GetProperty("obfs");
            Assert.AreEqual("gecko", singObfs.GetProperty("type").GetString());
            Assert.AreEqual(128, singObfs.GetProperty("min_packet_size").GetInt32());
            Assert.AreEqual(512, singObfs.GetProperty("max_packet_size").GetInt32());
        }

        var tuic = CreateDefault(EConfigType.TUIC);
        tuic.Address = "127.0.0.1";
        tuic.Port = 443;
        tuic.Username = Uuid;
        tuic.Password = "p";
        tuic.ProtoExtra.TuicUdpRelayMode = "quic";
        tuic.ProtoExtra.TuicReduceRtt = true;
        tuic.ProtoExtra.TuicDisableSni = true;
        var savedTuic = Save(tuic);
        using (var mihomoDoc = JsonDocument.Parse(Build("mihomo", savedTuic)))
        {
            var mihomoProxy = mihomoDoc.RootElement.GetProperty("proxies")[0];
            Assert.AreEqual("quic", mihomoProxy.GetProperty("udp-relay-mode").GetString());
            Assert.IsTrue(mihomoProxy.GetProperty("reduce-rtt").GetBoolean());
            Assert.IsTrue(mihomoProxy.GetProperty("disable-sni").GetBoolean());
        }
        using (var singDoc = JsonDocument.Parse(Build("sing-box", savedTuic)))
        {
            var singOutbound = singDoc.RootElement.GetProperty("outbounds")[0];
            Assert.AreEqual("quic", singOutbound.GetProperty("udp_relay_mode").GetString());
            Assert.IsTrue(singOutbound.GetProperty("zero_rtt_handshake").GetBoolean());
            Assert.IsTrue(singOutbound.GetProperty("tls").GetProperty("disable_sni").GetBoolean());
        }

        Assert.IsTrue(Netch.Constants.SsCiphers.Contains("aes-192-gcm"));
        var ss = CreateDefault(EConfigType.Shadowsocks);
        ss.Address = "127.0.0.1";
        ss.Port = 443;
        ss.Password = "pass";
        ss.ProtoExtra.SsPlugin = "v2ray-plugin";
        ss.ProtoExtra.SsPluginOptions = "mode=websocket;tls";
        ss.AllowInsecure = true;
        Assert.AreEqual(true, Save(ss).AllowInsecure);

        Assert.IsTrue(Netch.Constants.Flow.Contains("xtls-rprx-vision-udp443"));

        var tt = CreateDefault(EConfigType.TrustTunnel);
        tt.Address = "127.0.0.1";
        tt.Port = 443;
        tt.Username = "tt-u";
        tt.Password = "tt-p";
        tt.Sni = "tt.example";
        tt.ProtoExtra.TrustTunnelQuic = true;
        tt.ProtoExtra.CongestionControl = "bbr";
        tt.ProtoExtra.BbrProfile = "aggressive";
        tt.ProtoExtra.TrustTunnelHealthCheck = false;
        tt.ProtoExtra.TrustTunnelMaxConnections = 6;
        tt.ProtoExtra.TrustTunnelMinStreams = 2;
        using (var doc = JsonDocument.Parse(Build("mihomo", Save(tt))))
        {
            var proxy = doc.RootElement.GetProperty("proxies")[0];
            Assert.IsTrue(proxy.GetProperty("quic").GetBoolean());
            Assert.AreEqual("bbr", proxy.GetProperty("congestion-controller").GetString());
            Assert.AreEqual("aggressive", proxy.GetProperty("bbr-profile").GetString());
            Assert.IsFalse(proxy.GetProperty("health-check").GetBoolean());
            Assert.AreEqual(6, proxy.GetProperty("max-connections").GetInt32());
            Assert.AreEqual(2, proxy.GetProperty("min-streams").GetInt32());
        }

        var mieru = CreateDefault(EConfigType.Mieru);
        mieru.Address = "127.0.0.1";
        mieru.Port = 2010;
        mieru.Username = "m-u";
        mieru.Password = "m-p";
        mieru.ProtoExtra.MieruTransport = "udp";
        mieru.ProtoExtra.MieruPortRange = "2010:2020";
        mieru.ProtoExtra.MieruMultiplexing = "multiplexing_high";
        mieru.ProtoExtra.MieruHandshakeMode = "handShake_no_wait";
        using (var doc = JsonDocument.Parse(Build("mihomo", Save(mieru))))
        {
            var proxy = doc.RootElement.GetProperty("proxies")[0];
            Assert.IsFalse(proxy.TryGetProperty("port", out _));
            Assert.AreEqual("2010-2020", proxy.GetProperty("port-range").GetString());
            Assert.AreEqual("UDP", proxy.GetProperty("transport").GetString());
            Assert.AreEqual("MULTIPLEXING_HIGH", proxy.GetProperty("multiplexing").GetString());
            Assert.AreEqual("HANDSHAKE_NO_WAIT", proxy.GetProperty("handshake-mode").GetString());
        }

        var sq = CreateDefault(EConfigType.ShadowQUIC);
        sq.Address = "127.0.0.1";
        sq.Port = 443;
        sq.Username = "sq-u";
        sq.Password = "sq-p";
        sq.Sni = "sq.example";
        sq.ProtoExtra.ShadowQuicVersions = "V1, v2, v1";
        sq.ProtoExtra.ShadowQuicUdpOverStream = true;
        sq.ProtoExtra.ShadowQuicZeroRtt = true;
        sq.ProtoExtra.CongestionControl = "BBR";
        sq.ProtoExtra.BbrProfile = "Standard";
        sq.ProtoExtra.UpMbps = 100;
        sq.ProtoExtra.DownMbps = 200;
        using (var doc = JsonDocument.Parse(Build("mihomo", Save(sq))))
        {
            var proxy = doc.RootElement.GetProperty("proxies")[0];
            CollectionAssert.AreEqual(new[] { "v1", "v2" },
                proxy.GetProperty("quic-versions").EnumerateArray().Select(e => e.GetString()).ToArray());
            Assert.IsTrue(proxy.GetProperty("udp-over-stream").GetBoolean());
            Assert.IsTrue(proxy.GetProperty("zero-rtt").GetBoolean());
            Assert.AreEqual("100 Mbps", proxy.GetProperty("up").GetString());
            Assert.AreEqual("200 Mbps", proxy.GetProperty("down").GetString());
        }

        var awg = CreateDefault(EConfigType.AmneziaWG);
        awg.Address = "127.0.0.1";
        awg.Port = 51820;
        awg.Password = Key;
        awg.ProtoExtra.WgPublicKey = Key;
        awg.ProtoExtra.WgInterfaceAddress = "10.0.0.2/32";
        awg.ProtoExtra.AwgHeaderProtectionKey = Key;
        awg.ProtoExtra.AwgJc = 4;
        awg.ProtoExtra.AwgJmin = 40;
        awg.ProtoExtra.AwgJmax = 70;
        awg.ProtoExtra.AwgS1 = 15;
        awg.ProtoExtra.AwgS2 = 25;
        awg.ProtoExtra.AwgH1 = "100-200";
        awg.ProtoExtra.AwgRandomTrailers = true;
        var savedAwg = Save(awg);
        Assert.AreEqual(3, savedAwg.ProtoExtra.AwgVersion);
        using (var doc = JsonDocument.Parse(Build("mihomo", savedAwg)))
        {
            var proxy = doc.RootElement.GetProperty("proxies")[0];
            Assert.AreEqual("wireguard", proxy.GetProperty("type").GetString());
            var awgOpts = proxy.GetProperty("amnezia-wg-option");
            Assert.AreEqual(3, awgOpts.GetProperty("version").GetInt32());
            Assert.AreEqual(Key, awgOpts.GetProperty("header-protection-key").GetString());
            Assert.AreEqual("100-200", awgOpts.GetProperty("h1").GetString());
            Assert.IsTrue(awgOpts.GetProperty("random-trailers").GetBoolean());
        }
    }

    [TestMethod]
    public void EditorNormalizationClearsHiddenAndStaleParameters()
    {
        var vmess = CreateDefault(EConfigType.VMess);
        vmess.Address = "127.0.0.1";
        vmess.Port = 443;
        vmess.Password = Uuid;
        vmess.Network = "kcp";
        vmess.HeaderType = "dns";
        vmess.RequestHost = "dns.example";
        Assert.AreEqual("dns.example", Save(vmess).RequestHost);

        var trojan = new TrojanServer
        {
            Address = "127.0.0.1",
            Port = 443,
            Password = "p",
            ProtoExtra = { Flow = "" }
        };
        Assert.AreEqual("", Save(trojan).ProtoExtra.Flow);

        var hy2 = CreateDefault(EConfigType.Hysteria2);
        hy2.Address = "127.0.0.1";
        hy2.Port = 443;
        hy2.Password = "p";
        hy2.ProtoExtra.Obfs = "gecko";
        hy2.ProtoExtra.SalamanderPass = "pw";
        hy2.ProtoExtra.GeckoMinPacketSize = "500";
        hy2.ProtoExtra.GeckoMaxPacketSize = "100";
        Assert.IsNotNull(Validate(hy2));

        hy2.ProtoExtra.GeckoMinPacketSize = "128";
        hy2.ProtoExtra.GeckoMaxPacketSize = "512";
        hy2.ProtoExtra.Ports = "443,8443-8445";
        hy2.ProtoExtra.HopInterval = "10";
        hy2.CertSha = "legacy-sha";
        var savedHy2 = Save(hy2);
        Assert.AreEqual("10", savedHy2.ProtoExtra.HopInterval);

        savedHy2.ProtoExtra.Obfs = Netch.Constants.None;
        savedHy2.ProtoExtra.Ports = "  ";
        savedHy2.Cert = "pem";
        ServerEditorHelper.PrepareForSave(savedHy2);
        Assert.AreEqual("", savedHy2.ProtoExtra.SalamanderPass);
        Assert.AreEqual("", savedHy2.ProtoExtra.GeckoMinPacketSize);
        Assert.AreEqual("", savedHy2.ProtoExtra.GeckoMaxPacketSize);
        Assert.AreEqual("", savedHy2.ProtoExtra.HopInterval);
        Assert.AreEqual("", savedHy2.CertSha);

        var ss = new ShadowsocksServer
        {
            Address = "127.0.0.1",
            Port = 443,
            Password = "pass",
            AllowInsecure = true,
            ProtoExtra = { SsMethod = "aes-128-gcm", SsPlugin = "", SsPluginOptions = "" }
        };
        Assert.IsNull(Save(ss).AllowInsecure);

        var naive = CreateDefault(EConfigType.Naive);
        naive.Address = "127.0.0.1";
        naive.Port = 443;
        naive.Username = "u";
        naive.Password = "p";
        naive.ProtoExtra.NaiveQuic = false;
        naive.ProtoExtra.CongestionControl = "bbr2";
        Assert.AreEqual("", Save(naive).ProtoExtra.CongestionControl);

        var vless = CreateDefault(EConfigType.VLESS);
        vless.Address = "127.0.0.1";
        vless.Port = 443;
        vless.Password = Uuid;
        vless.Network = "ws";
        vless.StreamSecurity = "tls";
        vless.ProtoExtra.Flow = "xtls-rprx-vision";
        vless.Extra = """{"xPaddingBytes":"100"}""";
        var normalizedVless = Save(vless);
        Assert.AreEqual("", normalizedVless.ProtoExtra.Flow);
        Assert.AreEqual("", normalizedVless.Extra);

        var http = new HttpServer("127.0.0.1", 8080)
        {
            StreamSecurity = Netch.Constants.None,
            Sni = "sni.example",
            Alpn = "h2",
            Fingerprint = "chrome",
            AllowInsecure = true
        };
        var normalizedHttp = Save(http);
        Assert.AreEqual("", normalizedHttp.Sni);
        Assert.AreEqual("", normalizedHttp.Alpn);
        Assert.AreEqual("", normalizedHttp.Fingerprint);
        Assert.IsNull(normalizedHttp.AllowInsecure);

        var masque = new MASQUEServer
        {
            Address = "127.0.0.1",
            Port = 443,
            ProtoExtra = { MasqueHeaders = "Cookie: a=1; b=2" }
        };
        var savedMasque = Save(masque);
        Assert.AreEqual("Cookie: a=1; b=2\n", savedMasque.ProtoExtra.MasqueHeaders);
        using (var masqueDoc = JsonDocument.Parse(Build("sing-box", savedMasque)))
        {
            Assert.AreEqual("a=1; b=2", masqueDoc.RootElement.GetProperty("endpoints")[0].GetProperty("headers").GetProperty("Cookie")[0].GetString());
        }

        var tuicWithMasqueHeaders = new TUICServer
        {
            Address = "127.0.0.1",
            Port = 443,
            Username = Uuid,
            Password = "p",
            ProtoExtra = { MasqueHeaders = "X-Test: 1" }
        };
        ServerEditorHelper.PrepareForSave(tuicWithMasqueHeaders);
        Assert.AreEqual(Netch.Constants.StreamSecurity, tuicWithMasqueHeaders.StreamSecurity);
        Assert.AreEqual("X-Test: 1", tuicWithMasqueHeaders.ProtoExtra.MasqueHeaders);

        var anyTlsWithMasqueHeaders = new AnyTLSServer
        {
            Address = "127.0.0.1",
            Port = 443,
            Password = "p",
            ProtoExtra = { MasqueHeaders = "X-Test: 2" }
        };
        ServerEditorHelper.PrepareForSave(anyTlsWithMasqueHeaders);
        Assert.AreEqual(Netch.Constants.StreamSecurity, anyTlsWithMasqueHeaders.StreamSecurity);
        Assert.AreEqual("X-Test: 2", anyTlsWithMasqueHeaders.ProtoExtra.MasqueHeaders);

        Assert.IsTrue(Netch.Constants.SsrEncryptMethods.Count > 0);
        Assert.IsTrue(Netch.Constants.SsrProtocols.Count > 0);
        Assert.IsTrue(Netch.Constants.SsrObfs.Count > 0);

        var vlessLegacy = new VLESSServer
        {
            Address = "127.0.0.1",
            Port = 443,
            Password = Uuid,
            Network = "tcp",
            StreamSecurity = "tls",
            CertSha = "abc",
            EchConfigList = "ech1",
            MuxEnabled = false
        };
        var savedLegacy = Save(vlessLegacy);
        Assert.AreEqual("abc", savedLegacy.CertSha);
        Assert.AreEqual("ech1", savedLegacy.EchConfigList);
    }

    private static Server CreateDefault(EConfigType type) =>
        ServerEditorHelper.CreateDefault(ServerHelper.GetUtilByTypeName(type.ToString()));

    private static string Validate(Server server) =>
        ServerEditorHelper.ValidateForSave(server, ServerHelper.GetUtilByTypeName(server.ConfigType.ToString()));

    private static Server Save(Server server, bool resetShadowsocksTransport = false)
    {
        ServerEditorHelper.PrepareForSave(server, resetShadowsocksTransport);
        Assert.IsNull(Validate(server));
        return server;
    }

    private static void Populate(Server server)
    {
        switch (server.ConfigType)
        {
            case EConfigType.VLESS:
                server.Password = Uuid;
                server.Network = "xhttp";
                server.HeaderType = "packet-up";
                server.Path = "/test";
                break;
            case EConfigType.VMess:
                server.Password = Uuid;
                server.ProtoExtra.VmessSecurity = "aes-128-gcm";
                server.Network = "kcp";
                server.HeaderType = "srtp";
                server.Path = "ui-test-seed";
                break;
            case EConfigType.Trojan:
                server.Password = "ui-fixture";
                server.Network = "grpc";
                server.Path = "ui-test";
                break;
            case EConfigType.Shadowsocks:
                server.Password = "ui-fixture";
                server.ProtoExtra.SsPlugin = "v2ray-plugin";
                server.ProtoExtra.SsPluginOptions = "mode=websocket;host=localhost;path=/test";
                break;
            case EConfigType.ShadowsocksR:
                var ssr = (ShadowsocksRServer)server;
                ssr.Password = "ui-fixture";
                ssr.EncryptMethod = "aes-256-cfb";
                ssr.Protocol = "auth_chain_a";
                ssr.OBFS = "http_simple";
                ssr.ProtocolParam = "10:ui-fixture";
                break;
            case EConfigType.SOCKS:
                server.Username = "ui-user";
                server.Password = "ui-fixture";
                break;
            case EConfigType.HTTP:
                server.StreamSecurity = "tls";
                server.ProtoExtra.HttpHeaders = "Cookie: a=1; b=2";
                break;
            case EConfigType.Hysteria2:
                server.Password = "ui-fixture";
                server.ProtoExtra.Obfs = "salamander";
                server.ProtoExtra.SalamanderPass = "ui-obfs";
                server.ProtoExtra.Ports = "443,8443-8445";
                server.ProtoExtra.HopInterval = "10";
                server.ProtoExtra.UpMbps = 20;
                break;
            case EConfigType.TUIC:
                server.Username = Uuid;
                server.Password = "ui-fixture";
                server.ProtoExtra.CongestionControl = "cubic";
                break;
            case EConfigType.AnyTLS:
                server.Password = "ui-fixture";
                server.Sni = "localhost";
                break;
            case EConfigType.WireGuard:
                server.Password = Key;
                server.ProtoExtra.WgPublicKey = Key;
                server.ProtoExtra.WgPresharedKey = Key;
                server.ProtoExtra.WgInterfaceAddress = "10.10.10.2/24";
                server.ProtoExtra.WgReserved = "1,2,3";
                break;
            case EConfigType.Naive:
                server.Username = "ui-user";
                server.Password = "ui-fixture";
                server.ProtoExtra.InsecureConcurrency = 0;
                server.ProtoExtra.NaiveQuic = true;
                break;
            case EConfigType.MASQUE:
                server.ProtoExtra.MasquePath = "/.well-known/masque/ip/{target}/{ipproto}/";
                server.ProtoExtra.MasqueHeaders = "X-UI: test";
                break;
            case EConfigType.ShadowTLS:
                server.Password = "ui-ss-pw";
                server.Sni = "cdn.example";
                server.ProtoExtra.SsMethod = "aes-128-gcm";
                server.ProtoExtra.ShadowTlsVersion = 3;
                server.ProtoExtra.ShadowTlsPassword = "ui-stls-pw";
                server.ProtoExtra.Uot = true;
                break;
            case EConfigType.SSH:
                server.Username = "root";
                server.Password = "ui-ssh-pw";
                server.ProtoExtra.SshPrivateKey = "-----BEGIN OPENSSH PRIVATE KEY-----\ndGVzdA==\n-----END OPENSSH PRIVATE KEY-----";
                server.ProtoExtra.SshHostKeyAlgorithms = "ssh-ed25519";
                break;
            case EConfigType.TrustTunnel:
                server.Username = "tt-user";
                server.Password = "tt-pass";
                server.Sni = "tt.example";
                server.ProtoExtra.TrustTunnelHealthCheck = true;
                server.ProtoExtra.TrustTunnelMaxConnections = 4;
                break;
            case EConfigType.Mieru:
                server.Username = "mieru-user";
                server.Password = "mieru-pass";
                server.ProtoExtra.MieruTransport = "UDP";
                server.ProtoExtra.MieruMultiplexing = "MULTIPLEXING_MIDDLE";
                server.ProtoExtra.MieruHandshakeMode = "HANDSHAKE_NO_WAIT";
                break;
            case EConfigType.ShadowQUIC:
                server.Username = "sq-user";
                server.Password = "sq-pass";
                server.Sni = "sq.example";
                server.ProtoExtra.ShadowQuicVersions = "v1,v2";
                server.ProtoExtra.CongestionControl = "bbr";
                break;
            case EConfigType.AmneziaWG:
                server.Password = Key;
                server.ProtoExtra.WgPublicKey = Key;
                server.ProtoExtra.WgInterfaceAddress = "10.0.0.2/32";
                server.ProtoExtra.AwgJc = 4;
                server.ProtoExtra.AwgJmin = 40;
                server.ProtoExtra.AwgJmax = 70;
                server.ProtoExtra.AwgS1 = 15;
                server.ProtoExtra.AwgS2 = 25;
                server.ProtoExtra.AwgH1 = "100";
                server.ProtoExtra.AwgH2 = "200";
                server.ProtoExtra.AwgH3 = "300";
                server.ProtoExtra.AwgH4 = "400";
                break;
        }
    }

    private static string Build(string core, Server profile)
    {
        object config;
        if (core == "mihomo")
        {
            var builder = typeof(MihomoController).Assembly.GetType("Netch.Servers.MihomoConfig");
            config = builder.GetMethod("Build", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, [profile, "127.0.0.1", "127.0.0.1", (ushort)19081]);
        }
        else
        {
            var controller = (SingboxController)RuntimeHelpers.GetUninitializedObject(typeof(SingboxController));
            controller.LocalAddress = "127.0.0.1";
            controller.Socks5LocalPort = 19081;
            config = typeof(SingboxController).GetMethod("BuildConfig", PrivateInstance).Invoke(controller, [profile]);
        }
        return JsonSerializer.Serialize(config, Netch.Global.NewCustomJsonSerializerOptions());
    }

    private static void WriteConfig(string name, string content)
    {
        if (Environment.GetEnvironmentVariable("NETCH_EDITOR_CONFIG_DIR") is not { Length: > 0 } directory)
            return;
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, name + ".json"), content);
    }

    [TestMethod]
    public void Hysteria2ShadowTlsAndSshEditorNormalizationSavesExpectedCoreFields()
    {
        var hy2 = CreateDefault(EConfigType.Hysteria2);
        hy2.Address = "127.0.0.1";
        hy2.Port = 443;
        hy2.Password = "hy2-pw";
        hy2.ProtoExtra.Ports = "443,8443-8445";
        hy2.ProtoExtra.HopInterval = "10";
        hy2.ProtoExtra.HopIntervalMax = "30";
        hy2.ProtoExtra.BbrProfile = "aggressive";
        var savedHy2 = Save(hy2);
        Assert.AreEqual("30", savedHy2.ProtoExtra.HopIntervalMax);
        Assert.AreEqual("aggressive", savedHy2.ProtoExtra.BbrProfile);

        using (var hy2Sb = JsonDocument.Parse(Build("sing-box", savedHy2)))
        {
            var outbound = hy2Sb.RootElement.GetProperty("outbounds")[0];
            Assert.AreEqual("30s", outbound.GetProperty("hop_interval_max").GetString());
            Assert.AreEqual("aggressive", outbound.GetProperty("bbr_profile").GetString());
        }

        savedHy2.ProtoExtra.Ports = "";
        ServerEditorHelper.PrepareForSave(savedHy2);
        Assert.AreEqual("", savedHy2.ProtoExtra.HopIntervalMax);

        var stlsV1 = CreateDefault(EConfigType.ShadowTLS);
        stlsV1.Address = "127.0.0.1";
        stlsV1.Port = 443;
        stlsV1.Password = "ss-pw";
        stlsV1.Sni = "cdn.example";
        stlsV1.ProtoExtra.ShadowTlsVersion = 1;
        stlsV1.ProtoExtra.ShadowTlsPassword = "leftover-password";
        var savedStlsV1 = Save(stlsV1);
        Assert.AreEqual("", savedStlsV1.ProtoExtra.ShadowTlsPassword);
        using (var stlsSb = JsonDocument.Parse(Build("sing-box", savedStlsV1)))
        {
            var outbounds = stlsSb.RootElement.GetProperty("outbounds");
            Assert.AreEqual(3, outbounds.GetArrayLength());
            Assert.AreEqual("proxy-shadowtls", outbounds[0].GetProperty("detour").GetString());
            Assert.AreEqual("shadowtls", outbounds[1].GetProperty("type").GetString());
            Assert.IsFalse(outbounds[1].TryGetProperty("password", out _));
        }

        var vless = CreateDefault(EConfigType.VLESS);
        vless.Address = "vless.example";
        vless.Port = 443;
        vless.Password = Uuid;
        vless.StreamSecurity = "reality";
        vless.Sni = "cdn.example";
        vless.Alpn = "h2";
        vless.AllowInsecure = true;
        vless.Fingerprint = "chrome";
        vless.PublicKey = "6AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
        vless.ShortId = "ab";
        vless.SpiderX = "/sp";
        vless.StreamSecurity = "tls";
        ServerEditorHelper.PrepareForSave(vless);
        Assert.AreEqual("", vless.PublicKey);
        Assert.AreEqual("", vless.ShortId);
        Assert.AreEqual("", vless.SpiderX);
        Assert.AreEqual("chrome", vless.Fingerprint);

        vless.StreamSecurity = "none";
        ServerEditorHelper.PrepareForSave(vless);
        Assert.AreEqual("", vless.Sni);
        Assert.AreEqual("", vless.Alpn);
        Assert.IsNull(vless.AllowInsecure);
        Assert.AreEqual("", vless.Fingerprint);

        var sshCrLf = CreateDefault(EConfigType.SSH);
        sshCrLf.Address = "ssh.example";
        sshCrLf.Port = 22;
        sshCrLf.Username = "root";
        sshCrLf.ProtoExtra.SshPrivateKey = "-----BEGIN OPENSSH PRIVATE KEY-----\r\ndGVzdA==\r\n-----END OPENSSH PRIVATE KEY-----\r\n";
        var savedSsh = Save(sshCrLf);
        Assert.IsFalse(savedSsh.ProtoExtra.SshPrivateKey.Contains('\r'));

        var ttDisabledQuic = CreateDefault(EConfigType.TrustTunnel);
        ttDisabledQuic.Address = "tt.example";
        ttDisabledQuic.Port = 443;
        ttDisabledQuic.ProtoExtra.TrustTunnelQuic = false;
        ttDisabledQuic.ProtoExtra.CongestionControl = "bbr";
        ttDisabledQuic.ProtoExtra.BbrProfile = "aggressive";
        ttDisabledQuic.Cert = "pem";
        ttDisabledQuic.CertSha = "legacy-sha";
        ServerEditorHelper.PrepareForSave(ttDisabledQuic);
        Assert.AreEqual("", ttDisabledQuic.ProtoExtra.CongestionControl);
        Assert.AreEqual("", ttDisabledQuic.ProtoExtra.BbrProfile);
        Assert.AreEqual("", ttDisabledQuic.CertSha);

        var awgDowngrade = CreateDefault(EConfigType.AmneziaWG);
        awgDowngrade.Address = "awg.example";
        awgDowngrade.Port = 51820;
        awgDowngrade.Password = Key;
        awgDowngrade.ProtoExtra.WgPublicKey = Key;
        awgDowngrade.ProtoExtra.WgInterfaceAddress = "10.0.0.2/32";
        awgDowngrade.ProtoExtra.AwgVersion = 2;
        awgDowngrade.ProtoExtra.AwgHeaderProtectionKey = Key;
        awgDowngrade.ProtoExtra.AwgRandomTrailers = true;
        awgDowngrade.StreamSecurity = "tls";
        var savedAwgDowngrade = Save(awgDowngrade);
        Assert.AreEqual("", savedAwgDowngrade.ProtoExtra.AwgHeaderProtectionKey);
        Assert.IsNull(savedAwgDowngrade.ProtoExtra.AwgRandomTrailers);
        Assert.AreEqual("", savedAwgDowngrade.StreamSecurity);

        var awgTrailersOnly = CreateDefault(EConfigType.AmneziaWG);
        awgTrailersOnly.Address = "awg.example";
        awgTrailersOnly.Port = 51820;
        awgTrailersOnly.Password = Key;
        awgTrailersOnly.ProtoExtra.WgPublicKey = Key;
        awgTrailersOnly.ProtoExtra.WgInterfaceAddress = "10.0.0.2/32";
        awgTrailersOnly.ProtoExtra.AwgRandomTrailers = true;
        var savedAwgTrailers = Save(awgTrailersOnly);
        Assert.AreEqual(3, savedAwgTrailers.ProtoExtra.AwgVersion);
        Assert.AreEqual(true, savedAwgTrailers.ProtoExtra.AwgRandomTrailers);

        var ttCubic = CreateDefault(EConfigType.TrustTunnel);
        ttCubic.Address = "tt.example";
        ttCubic.Port = 443;
        ttCubic.ProtoExtra.TrustTunnelQuic = true;
        ttCubic.ProtoExtra.CongestionControl = "cubic";
        ttCubic.ProtoExtra.BbrProfile = "aggressive";
        var savedTtCubic = Save(ttCubic);
        Assert.AreEqual("", savedTtCubic.ProtoExtra.BbrProfile);

        var sqCubic = CreateDefault(EConfigType.ShadowQUIC);
        sqCubic.Address = "sq.example";
        sqCubic.Port = 443;
        sqCubic.Username = "u";
        sqCubic.Password = "p";
        sqCubic.ProtoExtra.CongestionControl = "cubic";
        sqCubic.ProtoExtra.BbrProfile = "aggressive";
        var savedSqCubic = Save(sqCubic);
        Assert.AreEqual("", savedSqCubic.ProtoExtra.BbrProfile);

        var mieruEdited = CreateDefault(EConfigType.Mieru);
        mieruEdited.Address = "mieru.example";
        mieruEdited.Port = 2010;
        mieruEdited.Username = "u";
        mieruEdited.Password = "p";
        mieruEdited.ProtoExtra.MieruPortRange = "3010-3020";
        var savedMieruEdited = Save(mieruEdited);
        Assert.AreEqual(3010, savedMieruEdited.Port);
        Assert.AreEqual("3010-3020", savedMieruEdited.ProtoExtra.MieruPortRange);
    }
}
