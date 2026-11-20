using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using System.Threading;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Netch.Enums;
using Netch.Forms;
using Netch.Interfaces;
using Netch.Models;
using Netch.Servers;

namespace Tests;

[TestClass]
public class NodeEditors
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private const string Uuid = "ac7f559b-d6d0-4bdc-96b1-5e0bd260779f";
    private static readonly string Key = Convert.ToBase64String(Enumerable.Range(1, 32).Select(n => (byte)n).ToArray());

    [TestMethod]
    public void ServerMenuHasEveryProtocolAtTheFirstLevel() => RunSta(() =>
    {
        using var main = new MainForm();
        var menu = (ToolStripMenuItem)typeof(MainForm)
            .GetField("ServerToolStripMenuItem", PrivateInstance).GetValue(main);
        var items = menu.DropDownItems.OfType<ToolStripMenuItem>().ToArray();
        Assert.AreEqual("ImportServersFromClipboardToolStripMenuItem", items[0].Name);
        Assert.AreEqual("ImportServersFromFileToolStripMenuItem", items[1].Name);
        Assert.IsNull(items[0].Tag);
        Assert.IsNull(items[1].Tag);
        Assert.IsTrue(items.All(item => !item.HasDropDownItems));
        var types = items.Skip(2).Select(item => ((IServerUtil)item.Tag).TypeName).ToArray();
        CollectionAssert.AreEqual(new[]
        {
            "VLESS", "VMess", "Trojan", "Shadowsocks", "ShadowsocksR", "SOCKS", "HTTP",
            "Hysteria2", "TUIC", "AnyTLS", "WireGuard", "Naive", "MASQUE"
        }, types);
        try
        {
            Netch.Utils.i18N.Load("zh-CN");
            typeof(MainForm).GetMethod("TranslateControls", PrivateInstance).Invoke(main, null);
            Assert.AreEqual("添加 VLESS…", items[2].Text);
        }
        finally { Netch.Utils.i18N.Load("en-US"); }
    });

    [TestMethod]
    public void HelpMenuListsTheCurrentCoreWebsites() => RunSta(() =>
    {
        using var main = new MainForm();
        var menu = (ToolStripMenuItem)typeof(MainForm)
            .GetField("HelpToolStripMenuItem", PrivateInstance).GetValue(main);
        var items = menu.DropDownItems.OfType<ToolStripMenuItem>().Select(item => item.Text).ToArray();
        CollectionAssert.AreEqual(new[]
        {
            "Check for update", "Mihomo Website", "sing-box Website", "Xray-core Website",
            "TUN2Socks Website", "WinTUN WebSite", "pcap2socks Website", "FAQ"
        }, items);
        try
        {
            Netch.Utils.i18N.Load("zh-CN");
            typeof(MainForm).GetMethod("TranslateControls", PrivateInstance).Invoke(main, null);
            CollectionAssert.AreEqual(new[] { "Mihomo 官网", "sing-box 官网", "Xray-core 官网" },
                menu.DropDownItems.OfType<ToolStripMenuItem>().Skip(1).Take(3).Select(item => item.Text).ToArray());
        }
        finally { Netch.Utils.i18N.Load("en-US"); }
    });

    [TestMethod]
    public void TransportAndTlsChangesKeepTheEditorWindowStable() => RunSta(() =>
    {
        using var editor = CreateEditor(EConfigType.VLESS);
        Prepare(editor);
        var sizeChanges = 0;
        editor.ClientSizeChanged += (_, _) => sizeChanges++;
        var original = editor.ClientSize;
        Select(editor, "Network", "xhttp");
        Select(editor, "TLSSecure", "tls");
        Select(editor, "TLSSecure", "reality");
        Select(editor, "Network", "ws");
        Assert.AreEqual(original, editor.ClientSize);
        Assert.AreEqual(0, sizeChanges, "Changing protocol fields resized the editor window.");
        Assert.AreEqual("reality", Find(editor, "TLSSecureComboBox").Text);
        Set(editor, "Address", "127.0.0.1");
        Set(editor, "Port", "443");
        Set(editor, "UUID", Uuid);
        Set(editor, "PublicKey", "6AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
        Assert.AreEqual(false, typeof(ServerForm).GetMethod("TrySave", PrivateInstance).Invoke(editor, null));
        Select(editor, "TLSSecure", "tls");
        Assert.AreEqual("tls", Save(editor).StreamSecurity);
    });

    [TestMethod]
    public void EditingExistingShadowsocksPluginUsesTheNewPathAndTls() => RunSta(() =>
    {
        var node = new ShadowsocksServer
        {
            Address = "127.0.0.1", Port = 443, Password = "ui-fixture",
            Network = "ws", Path = "/old", RequestHost = "old.example", StreamSecurity = "tls",
            ProtoExtra = { SsMethod = "aes-128-gcm", SsPlugin = "v2ray-plugin", SsPluginOptions = "mode=websocket;path=/old;tls" }
        };
        using var editor = CreateEditor(EConfigType.Shadowsocks, node);
        Prepare(editor);
        Set(editor, "PluginOptions", @"mode=websocket;path=/new?a\=1\,2");
        using var config = JsonDocument.Parse(Build("mihomo", Save(editor)));
        var options = config.RootElement.GetProperty("proxies")[0].GetProperty("plugin-opts");
        Assert.AreEqual("/new?a=1,2", options.GetProperty("path").GetString());
        Assert.AreEqual("127.0.0.1", options.GetProperty("host").GetString());
        Assert.IsFalse(options.GetProperty("tls").GetBoolean());
        WriteConfig("Shadowsocks-Edited", config.RootElement.GetRawText());
    });

    [TestMethod]
    public void WireGuardFallbackWritesBareAddressesAsHostPrefixes() => RunSta(() =>
    {
        using var editor = CreateEditor(EConfigType.WireGuard);
        Prepare(editor);
        Set(editor, "Address", "127.0.0.1");
        Set(editor, "Port", "51820");
        Populate(editor, EConfigType.WireGuard);
        Set(editor, "LocalAddresses", "10.10.10.2,10.10.10.3/24,2001:db8::1");
        using var config = JsonDocument.Parse(Build("sing-box", Save(editor)));
        var addresses = config.RootElement.GetProperty("endpoints")[0].GetProperty("address");
        CollectionAssert.AreEqual(new[] { "10.10.10.2/32", "10.10.10.3/24", "2001:db8::1/128" },
            addresses.EnumerateArray().Select(address => address.GetString()).ToArray());
        WriteConfig("WireGuard-Singbox", config.RootElement.GetRawText());
    });

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
    public void EditorSavesParametersUsedByItsCore(EConfigType type, string core, string expectedField) => RunSta(() =>
    {
        using var editor = CreateEditor(type);
        Prepare(editor);
        Set(editor, "Address", "127.0.0.1");
        Set(editor, "Port", "443");
        Populate(editor, type);
        var saved = Save(editor);
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

        using var reopened = CreateEditor(type, saved);
        Prepare(reopened);
        Assert.AreEqual("443", Find(reopened, "PortTextBox").Text);
        var generated = Build(core, saved);
        Assert.AreEqual(generated, Build(core, Save(reopened)), "Reopening and saving changed the effective configuration.");
        using var doc = JsonDocument.Parse(generated);
        var proxy = core == "mihomo" ? doc.RootElement.GetProperty("proxies")[0]
            : doc.RootElement.GetProperty(type == EConfigType.MASQUE ? "endpoints" : "outbounds")[0];
        Assert.IsTrue(proxy.TryGetProperty(expectedField, out _), $"The generated {core} proxy omits {expectedField}.");
        if (type == EConfigType.HTTP)
            Assert.AreEqual("a=1; b=2", proxy.GetProperty("headers").GetProperty("Cookie").GetString());
        if (type == EConfigType.Naive)
            Assert.IsFalse(proxy.GetProperty("tls").TryGetProperty("insecure", out _));
        WriteConfig(type.ToString(), generated);
    });

    [TestMethod]
    public void XhttpEchFieldsSaveSeparatelyAndSocksRejectsPartialAuth() => RunSta(() =>
    {
        using var vless = CreateEditor(EConfigType.VLESS);
        Prepare(vless);
        Set(vless, "Address", "127.0.0.1");
        Set(vless, "Port", "443");
        Set(vless, "UUID", Uuid);
        Select(vless, "Network", "xhttp");
        Select(vless, "TLSSecure", "tls");
        Set(vless, "XhttpExtra", """{"xPaddingBytes":"100-1000"}""");
        var saved = Save(vless);
        var config = V2rayConfigUtils.GenerateClientConfig(saved, "127.0.0.1", 19081);
        WriteConfig("VLESS-Xray", JsonSerializer.Serialize(config, Netch.Global.NewCustomJsonSerializerOptions()));
        Set(vless, "EchConfigList", "ech-fixture");
        Select(vless, "EchForceQuery", "full");
        saved = Save(vless);
        Assert.AreEqual("ech-fixture", saved.EchConfigList);
        Assert.AreEqual("full", saved.EchForceQuery);
        var tls = V2rayConfigUtils.GenerateClientConfig(saved, "127.0.0.1", 19081).outbounds[0].streamSettings.tlsSettings;
        Assert.AreEqual("ech-fixture", tls.echConfigList);
        Assert.AreEqual("full", tls.echForceQuery);

        using var socks = CreateEditor(EConfigType.SOCKS);
        Prepare(socks);
        Set(socks, "Address", "127.0.0.1");
        Set(socks, "Port", "1080");
        Set(socks, "Username", "test");
        Assert.AreEqual(false, typeof(ServerForm).GetMethod("TrySave", PrivateInstance).Invoke(socks, null));
        Assert.AreEqual(1, Netch.Global.Settings.Server.Count);
    });

    private static ServerForm CreateEditor(EConfigType type, Server existing = null)
    {
        if (type is EConfigType.TUIC or EConfigType.AnyTLS or EConfigType.Naive or EConfigType.MASQUE)
            return new SingboxServerForm(existing, type);
        if (type == EConfigType.ShadowsocksR)
        {
            var editor = typeof(ServerEditors).GetNestedType("ShadowsocksREditor", BindingFlags.NonPublic);
            return (ServerForm)Activator.CreateInstance(editor, existing ?? new ShadowsocksRServer());
        }
        var name = type switch { EConfigType.SOCKS => "Socks", EConfigType.HTTP => "Http", _ => type.ToString() };
        return (ServerForm)Activator.CreateInstance(typeof(ServerForm).Assembly.GetType($"Netch.Servers.{name}Form"), [existing]);
    }

    private static void Prepare(ServerForm form) => typeof(ServerForm).GetMethod("Prepare", PrivateInstance).Invoke(form, null);

    private static Server Save(ServerForm form)
    {
        Assert.AreEqual(true, typeof(ServerForm).GetMethod("TrySave", PrivateInstance).Invoke(form, null),
            string.Join(", ", form.Controls.Find("AddressTextBox", true).Select(c => c.Text)));
        return Netch.Global.Settings.Server.Last();
    }

    private static Control Find(Control root, string name) => root.Controls.Find(name, true).Single();

    private static void Set(Control form, string name, string value) => Find(form, name + "TextBox").Text = value;

    private static void Select(Control form, string name, string value)
    {
        var field = (ComboBox)Find(form, name + "ComboBox");
        Assert.IsTrue(field.Items.Contains(value), $"{name} lacks {value}");
        field.SelectedItem = value;
    }

    [TestMethod]
    public void EditorsExposeAndSaveAllProtocolSpecificParameters() => RunSta(() =>
    {
        using (var anyTls = CreateEditor(EConfigType.AnyTLS))
        {
            Prepare(anyTls);
            Set(anyTls, "Address", "127.0.0.1");
            Set(anyTls, "Port", "443");
            Set(anyTls, "Password", "pass");
            Set(anyTls, "IdleSessionCheckInterval", "25");
            Set(anyTls, "IdleSessionTimeout", "60");
            Set(anyTls, "MinIdleSession", "2");
            var saved = Save(anyTls);
            using var doc = JsonDocument.Parse(Build("mihomo", saved));
            var proxy = doc.RootElement.GetProperty("proxies")[0];
            Assert.AreEqual(25, proxy.GetProperty("idle-session-check-interval").GetInt32());
            Assert.AreEqual(60, proxy.GetProperty("idle-session-timeout").GetInt32());
            Assert.AreEqual(2, proxy.GetProperty("min-idle-session").GetInt32());
        }

        using (var naive = CreateEditor(EConfigType.Naive))
        {
            Prepare(naive);
            Set(naive, "Address", "127.0.0.1");
            Set(naive, "Port", "443");
            Set(naive, "Username", "u");
            Set(naive, "Password", "p");
            ((CheckBox)Find(naive, "NaiveQuicCheckBox")).Checked = true;
            Select(naive, "CongestionControl", "bbr2");
            ((CheckBox)Find(naive, "UotCheckBox")).Checked = true;
            var saved = Save(naive);
            using var doc = JsonDocument.Parse(Build("sing-box", saved));
            var outbound = doc.RootElement.GetProperty("outbounds")[0];
            Assert.AreEqual("bbr2", outbound.GetProperty("quic_congestion_control").GetString());
            Assert.IsTrue(outbound.GetProperty("udp_over_tcp").GetBoolean());
        }

        using (var hy2 = CreateEditor(EConfigType.Hysteria2))
        {
            Prepare(hy2);
            Set(hy2, "Address", "127.0.0.1");
            Set(hy2, "Port", "443");
            Set(hy2, "Auth", "p");
            Select(hy2, "Obfs", "gecko");
            Set(hy2, "ObfsPassword", "obfs-pw");
            Set(hy2, "GeckoMinPacketSize", "128");
            Set(hy2, "GeckoMaxPacketSize", "512");
            var saved = Save(hy2);
            using var mihomoDoc = JsonDocument.Parse(Build("mihomo", saved));
            var mihomoProxy = mihomoDoc.RootElement.GetProperty("proxies")[0];
            Assert.AreEqual("gecko", mihomoProxy.GetProperty("obfs").GetString());
            Assert.AreEqual(128, mihomoProxy.GetProperty("obfs-min-packet-size").GetInt32());
            Assert.AreEqual(512, mihomoProxy.GetProperty("obfs-max-packet-size").GetInt32());

            using var singDoc = JsonDocument.Parse(Build("sing-box", saved));
            var singObfs = singDoc.RootElement.GetProperty("outbounds")[0].GetProperty("obfs");
            Assert.AreEqual("gecko", singObfs.GetProperty("type").GetString());
            Assert.AreEqual(128, singObfs.GetProperty("min_packet_size").GetInt32());
            Assert.AreEqual(512, singObfs.GetProperty("max_packet_size").GetInt32());
        }

        using (var tuic = CreateEditor(EConfigType.TUIC))
        {
            Prepare(tuic);
            Set(tuic, "Address", "127.0.0.1");
            Set(tuic, "Port", "443");
            Set(tuic, "Username", Uuid);
            Set(tuic, "Password", "p");
            Select(tuic, "UdpRelayMode", "quic");
            ((CheckBox)Find(tuic, "ReduceRttCheckBox")).Checked = true;
            ((CheckBox)Find(tuic, "DisableSniCheckBox")).Checked = true;
            var saved = Save(tuic);
            using var mihomoDoc = JsonDocument.Parse(Build("mihomo", saved));
            var mihomoProxy = mihomoDoc.RootElement.GetProperty("proxies")[0];
            Assert.AreEqual("quic", mihomoProxy.GetProperty("udp-relay-mode").GetString());
            Assert.IsTrue(mihomoProxy.GetProperty("reduce-rtt").GetBoolean());
            Assert.IsTrue(mihomoProxy.GetProperty("disable-sni").GetBoolean());

            using var singDoc = JsonDocument.Parse(Build("sing-box", saved));
            var singOutbound = singDoc.RootElement.GetProperty("outbounds")[0];
            Assert.AreEqual("quic", singOutbound.GetProperty("udp_relay_mode").GetString());
            Assert.IsTrue(singOutbound.GetProperty("zero_rtt_handshake").GetBoolean());
            Assert.IsTrue(singOutbound.GetProperty("tls").GetProperty("disable_sni").GetBoolean());
        }

        using (var ss = CreateEditor(EConfigType.Shadowsocks))
        {
            Prepare(ss);
            var cipherBox = (ComboBox)Find(ss, "EncryptMethodComboBox");
            Assert.AreEqual(Netch.Constants.SsCiphers.Count, cipherBox.Items.Count);
            Assert.IsTrue(cipherBox.Items.Contains("aes-192-gcm"));
            Set(ss, "Address", "127.0.0.1");
            Set(ss, "Port", "443");
            Set(ss, "Password", "pass");
            Select(ss, "Plugin", "v2ray-plugin");
            Set(ss, "PluginOptions", "mode=websocket;tls");
            Select(ss, "AllowInsecure", "true");
            var saved = Save(ss);
            Assert.AreEqual(true, saved.AllowInsecure);
        }

        using (var vless = CreateEditor(EConfigType.VLESS))
        {
            Prepare(vless);
            var networkBox = (ComboBox)Find(vless, "NetworkComboBox");
            Assert.IsFalse(networkBox.Items.Contains("hysteria"));
            var flowBox = (ComboBox)Find(vless, "FlowComboBox");
            Assert.IsTrue(flowBox.Items.Contains("xtls-rprx-vision-udp443"));
        }
    });

    [TestMethod]
    public void EditorsHandleDynamicVisibilityValidationAndClearing() => RunSta(() =>
    {
        static bool IsHidden(ServerForm form, Control control) =>
            ((HashSet<Control>)typeof(ServerForm).GetField("_hidden", PrivateInstance).GetValue(form)).Contains(control);

        using (var vmess = CreateEditor(EConfigType.VMess))
        {
            Prepare(vmess);
            Set(vmess, "Address", "127.0.0.1");
            Set(vmess, "Port", "443");
            Set(vmess, "UserId", Uuid);
            Select(vmess, "Network", "kcp");
            Select(vmess, "TransportMode", "dns");
            var hostBox = Find(vmess, "HostTextBox");
            Assert.IsFalse(IsHidden(vmess, hostBox));
            Set(vmess, "Host", "dns.example");
            Assert.AreEqual("dns.example", Save(vmess).RequestHost);
            Select(vmess, "TransportMode", "srtp");
            Assert.IsTrue(IsHidden(vmess, hostBox));
        }

        using (var trojan = CreateEditor(EConfigType.Trojan, new TrojanServer
        {
            Address = "127.0.0.1", Port = 443, Password = "p", ProtoExtra = { Flow = "xtls-rprx-direct" }
        }))
        {
            Prepare(trojan);
            Assert.IsNotNull(Find(trojan, "LegacyFlowComboBox"));
            Select(trojan, "LegacyFlow", "");
            Assert.AreEqual("", Save(trojan).ProtoExtra.Flow);
        }

        using (var hy2 = CreateEditor(EConfigType.Hysteria2))
        {
            Prepare(hy2);
            Set(hy2, "Address", "127.0.0.1");
            Set(hy2, "Port", "443");
            Set(hy2, "Auth", "p");
            Select(hy2, "Obfs", "gecko");
            Set(hy2, "ObfsPassword", "pw");
            Set(hy2, "GeckoMinPacketSize", "500");
            Set(hy2, "GeckoMaxPacketSize", "100");
            Assert.AreEqual(false, typeof(ServerForm).GetMethod("TrySave", PrivateInstance).Invoke(hy2, null));

            Set(hy2, "GeckoMinPacketSize", "128");
            Set(hy2, "GeckoMaxPacketSize", "512");
            var saved = Save(hy2);
            using var reopened = CreateEditor(EConfigType.Hysteria2, saved);
            Prepare(reopened);
            Select(reopened, "Obfs", "none");
            var cleared = Save(reopened);
            Assert.AreEqual("", cleared.ProtoExtra.SalamanderPass);
            Assert.AreEqual("", cleared.ProtoExtra.GeckoMinPacketSize);
            Assert.AreEqual("", cleared.ProtoExtra.GeckoMaxPacketSize);
        }

        using (var ss = CreateEditor(EConfigType.Shadowsocks, new ShadowsocksServer
        {
            Address = "127.0.0.1", Port = 443, Password = "pass",
            AllowInsecure = true, ProtoExtra = { SsMethod = "aes-128-gcm", SsPlugin = "v2ray-plugin", SsPluginOptions = "mode=websocket;tls" }
        }))
        {
            Prepare(ss);
            Select(ss, "Plugin", "");
            Assert.IsTrue(IsHidden(ss, Find(ss, "AllowInsecureComboBox")));
            Assert.IsNull(Save(ss).AllowInsecure);
        }

        using (var ssr = CreateEditor(EConfigType.ShadowsocksR))
        {
            Prepare(ssr);
            Assert.AreEqual(Netch.Constants.SsrEncryptMethods.Count, ((ComboBox)Find(ssr, "EncryptMethodComboBox")).Items.Count);
            Assert.AreEqual(Netch.Constants.SsrProtocols.Count, ((ComboBox)Find(ssr, "ProtocolComboBox")).Items.Count);
            Assert.AreEqual(Netch.Constants.SsrObfs.Count, ((ComboBox)Find(ssr, "OBFSComboBox")).Items.Count);
        }

        foreach (var type in new[] { EConfigType.Hysteria2, EConfigType.TUIC, EConfigType.AnyTLS, EConfigType.MASQUE })
        {
            using var editor = CreateEditor(type);
            Prepare(editor);
            Assert.IsFalse(IsHidden(editor, Find(editor, "FingerprintComboBox")), $"{type} should expose FingerprintComboBox");
        }
        using (var naive = CreateEditor(EConfigType.Naive))
        {
            Prepare(naive);
            Assert.AreEqual(0, naive.Controls.Find("FingerprintComboBox", true).Length);
            Assert.AreEqual(0, naive.Controls.Find("AllowInsecureComboBox", true).Length);
        }

        using (var vlessLegacy = CreateEditor(EConfigType.VLESS, new VLESSServer
        {
            Address = "127.0.0.1", Port = 443, Password = Uuid, Network = "tcp",
            StreamSecurity = "tls", CertSha = "abc", EchConfigList = "ech1"
        }))
        {
            Prepare(vlessLegacy);
            Assert.IsFalse(IsHidden(vlessLegacy, Find(vlessLegacy, "CertShaTextBox")));
            Assert.IsFalse(IsHidden(vlessLegacy, Find(vlessLegacy, "EchConfigListTextBox")));
            var saved = Save(vlessLegacy);
            Assert.AreEqual("abc", saved.CertSha);
            Assert.AreEqual("ech1", saved.EchConfigList);
        }
    });

    private static void Populate(ServerForm form, EConfigType type)
    {
        switch (type)
        {
            case EConfigType.VLESS:
                Set(form, "UUID", Uuid);
                Select(form, "Network", "xhttp");
                Select(form, "TransportMode", "packet-up");
                Set(form, "Path", "/test");
                break;
            case EConfigType.VMess:
                Set(form, "UserId", Uuid);
                Select(form, "Encryption", "aes-128-gcm");
                Select(form, "Network", "kcp");
                Select(form, "TransportMode", "srtp");
                Set(form, "Path", "ui-test-seed");
                break;
            case EConfigType.Trojan:
                Set(form, "Password", "ui-fixture");
                Select(form, "Network", "grpc");
                Set(form, "Path", "ui-test");
                break;
            case EConfigType.Shadowsocks:
                Set(form, "Password", "ui-fixture");
                Select(form, "Plugin", "v2ray-plugin");
                Set(form, "PluginOptions", "mode=websocket;host=localhost;path=/test");
                break;
            case EConfigType.ShadowsocksR:
                Set(form, "Password", "ui-fixture");
                Select(form, "Protocol", "auth_chain_a");
                Select(form, "OBFS", "http_simple");
                Set(form, "ProtocolParam", "10:ui-fixture");
                break;
            case EConfigType.SOCKS:
                Set(form, "Username", "ui-user");
                Set(form, "Password", "ui-fixture");
                break;
            case EConfigType.HTTP:
                Select(form, "TLSSecure", "HTTPS");
                Set(form, "HttpHeaders", "Cookie: a=1; b=2");
                break;
            case EConfigType.Hysteria2:
                Set(form, "Auth", "ui-fixture");
                Select(form, "Obfs", "salamander");
                Set(form, "ObfsPassword", "ui-obfs");
                Set(form, "PortHoppingRange", "443,8443-8445");
                Set(form, "HopInterval", "10");
                Set(form, "UpMbps", "20");
                break;
            case EConfigType.TUIC:
                Set(form, "Username", Uuid);
                Set(form, "Password", "ui-fixture");
                Select(form, "CongestionControl", "cubic");
                break;
            case EConfigType.AnyTLS:
                Set(form, "Password", "ui-fixture");
                Set(form, "Sni", "localhost");
                break;
            case EConfigType.WireGuard:
                Set(form, "PrivateKey", Key);
                Set(form, "PublicKey", Key);
                Set(form, "PresharedKey", Key);
                Set(form, "LocalAddresses", "10.10.10.2/24");
                Set(form, "Reserved", "1,2,3");
                break;
            case EConfigType.Naive:
                Set(form, "Username", "ui-user");
                Set(form, "Password", "ui-fixture");
                Set(form, "InsecureConcurrency", "0");
                ((CheckBox)Find(form, "NaiveQuicCheckBox")).Checked = true;
                break;
            case EConfigType.MASQUE:
                Set(form, "MasquePath", "/.well-known/masque/ip/{target}/{ipproto}/");
                Set(form, "MasqueHeaders", "X-UI: test");
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
        if (Environment.GetEnvironmentVariable("NETCH_EDITOR_CONFIG_DIR") is not { Length: > 0 } directory) return;
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, name + ".json"), content);
    }

    private static void RunSta(Action test)
    {
        ExceptionDispatchInfo error = null;
        var thread = new Thread(() =>
        {
            var original = Netch.Global.Settings;
            try
            {
                Netch.Global.Settings = new Setting();
                test();
            }
            catch (Exception exception) { error = ExceptionDispatchInfo.Capture(exception); }
            finally { Netch.Global.Settings = original; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        error?.Throw();
    }
}
