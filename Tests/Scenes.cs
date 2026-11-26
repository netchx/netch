using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Netch.Controllers;
using Netch.Interfaces;
using Netch.Models;
using Netch.Models.Modes;
using Netch.Models.Modes.ProcessMode;
using Netch.Models.Modes.ShareMode;
using Netch.Models.Modes.TunMode;
using Netch.Servers;
using Netch.Services;
using Netch.Utils;

namespace Tests;

[TestClass]
public class Scenes
{
    private Setting _savedSettings;

    [TestInitialize]
    public void SaveGlobals() => _savedSettings = Netch.Global.Settings;

    [TestCleanup]
    public void RestoreGlobals()
    {
        Netch.Global.Settings = _savedSettings;
        Netch.Global.Modes.Clear();
    }

    [TestMethod]
    public void BandwidthEventsPreserveLegacyCountAndExposeDirections()
    {
        ulong legacy = 0;
        (ulong Download, ulong Upload) directions = default;
        Action<ulong> legacyHandler = value => legacy = value;
        Action<ulong, ulong> directionalHandler = (down, up) => directions = (down, up);
        AppEvents.BandwidthUpdated += legacyHandler;
        AppEvents.BandwidthDirectionsUpdated += directionalHandler;
        try
        {
            AppEvents.ReportBandwidth(1400, 1000, 400);
            Assert.AreEqual(1400UL, legacy);
            Assert.AreEqual((1000UL, 400UL), directions);
            Assert.AreEqual("512 B", Bandwidth.Compute(512));
        }
        finally
        {
            AppEvents.BandwidthUpdated -= legacyHandler;
            AppEvents.BandwidthDirectionsUpdated -= directionalHandler;
        }
    }

    [TestMethod]
    public void ProfilesBecomeScenesOnce()
    {
        var hk = new SocksServer { Remarks = "HK 01", Address = "hk.example", Port = 1080 };
        var game = new Redirector { FullName = Path.Combine(ModeService.Instance.ModeDirectoryFullName, "Game", "Steam.json") };
        game.Remark["en"] = "Steam";
        var global = new TunMode { FullName = Path.Combine(ModeService.Instance.ModeDirectoryFullName, "Global.json") };
        global.Remark["zh-CN"] = "全局";
        Netch.Global.Modes.AddRange(new Netch.Models.Modes.Mode[] { game, global });

        Netch.Global.Settings = new Setting
        {
            Server = { hk },
            Profiles =
            {
                new Profile { Index = 1, ProfileName = "", ServerRemark = "HK 01", ModeRemark = "全局" },
                new Profile { Index = 0, ProfileName = "Games", ServerRemark = "HK 01", ModeRemark = "Steam" },
                new Profile { Index = 2, ProfileName = "Gone", ServerRemark = "removed node", ModeRemark = "Steam" }
            }
        };

        Assert.IsTrue(SceneService.Initialize());
        var scenes = SceneService.Scenes;

        CollectionAssert.AreEqual(new[] { "Games", "全局", "Gone" }, scenes.Select(s => s.Name).ToArray());
        Assert.AreEqual(@"Game\Steam.json", scenes[0].ModeId);
        Assert.AreSame(game, SceneService.FindMode(scenes[0]));
        Assert.AreSame(hk, SceneService.FindServer(scenes[0]));
        Assert.AreSame(global, SceneService.FindMode(scenes[1]));
        Assert.AreEqual(SceneIcon.Globe, scenes[1].Icon);
        Assert.IsNull(SceneService.FindServer(scenes[2]), "a profile whose node is gone keeps its scene, without a node");

        scenes.Clear();
        Assert.IsFalse(SceneService.Initialize(), "deleting every scene must not bring the profiles back");
    }

    [TestMethod]
    [DataRow(false, true)]
    [DataRow(true, true)]
    [DataRow(false, false)]
    [DataRow(true, false)]
    public void LegacyMigrationKeepsTheSelectedPair(bool savedAsProfile, bool startWhenOpened)
    {
        var first = new SocksServer { Remarks = "First node" };
        var selected = new SocksServer { Remarks = "Selected node" };
        var firstMode = new Redirector { FullName = Path.Combine(ModeService.Instance.ModeDirectoryFullName, "First.json") };
        firstMode.Remark["en"] = "First mode";
        var selectedMode = new Redirector { FullName = Path.Combine(ModeService.Instance.ModeDirectoryFullName, "Selected.json") };
        selectedMode.Remark["en"] = "Selected mode";
        Netch.Global.Modes.Add(firstMode);
        Netch.Global.Modes.Add(selectedMode);
        var settings = Netch.Global.Settings = new Setting
        {
            Server = { first, selected },
            ServerComboBoxSelectedIndex = 1,
            ModeComboBoxSelectedIndex = 1,
            StartWhenOpened = startWhenOpened,
            Profiles = { new Profile { Index = 0, ProfileName = "Other profile", ServerRemark = first.Remarks, ModeRemark = "First mode" } }
        };
        if (savedAsProfile)
            settings.Profiles.Add(new Profile { Index = 1, ProfileName = "Selected profile", ServerRemark = selected.Remarks, ModeRemark = "Selected mode" });

        Assert.IsTrue(SceneService.Initialize());
        Assert.AreEqual("Other profile", SceneService.Scenes[0].Name);
        Assert.AreEqual(2, SceneService.Scenes.Count);
        var startup = SceneService.Scenes.Single(scene => scene.Id == settings.StartupSceneId);
        Assert.AreSame(selected, SceneService.FindServer(startup));
        Assert.AreSame(selectedMode, SceneService.FindMode(startup));
        Assert.AreEqual(savedAsProfile ? "Selected profile" : "Selected mode", startup.Name);
        Assert.AreEqual(startWhenOpened, settings.StartWhenOpened);
        Assert.IsFalse(SceneService.Initialize());
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void LegacySelectionWithoutProfilesCanBecomeAScene(bool startWhenOpened)
    {
        var node = new SocksServer { Remarks = "Selected node" };
        var mode = new TunMode { FullName = Path.Combine(ModeService.Instance.ModeDirectoryFullName, "First.json") };
        mode.Remark["en"] = "First mode";
        Netch.Global.Modes.Add(mode);
        var settings = Netch.Global.Settings = new Setting
        {
            Server = { node },
            ServerComboBoxSelectedIndex = startWhenOpened ? -1 : 0,
            StartWhenOpened = startWhenOpened
        };

        Assert.IsTrue(SceneService.Initialize());
        var scene = SceneService.Scenes.Single();
        Assert.AreSame(node, SceneService.FindServer(scene));
        Assert.AreSame(mode, SceneService.FindMode(scene));
        Assert.AreEqual(SceneIcon.Globe, scene.Icon);
        Assert.AreEqual(scene.Id, settings.StartupSceneId);
        Assert.AreEqual(startWhenOpened, settings.StartWhenOpened);
    }

    [TestMethod]
    public void ModeListReflectsSuccessfulFileChangesOnly()
    {
        var directory = Directory.CreateTempSubdirectory();
        var mode = new Redirector { FullName = Path.Combine(directory.FullName, "missing", "mode.json") };
        try
        {
            Assert.ThrowsExactly<DirectoryNotFoundException>(() => ModeService.Instance.Add(mode));
            Assert.IsFalse(Netch.Global.Modes.Contains(mode));

            mode.FullName = Path.Combine(directory.FullName, "mode.json");
            ModeService.Instance.Add(mode);
            using (File.Open(mode.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                Assert.ThrowsExactly<IOException>(() => ModeService.Delete(mode));
                Assert.IsTrue(Netch.Global.Modes.Contains(mode));
            }
            ModeService.Delete(mode);
            Assert.IsFalse(Netch.Global.Modes.Contains(mode));
            Assert.IsFalse(File.Exists(mode.FullName));
        }
        finally
        {
            File.Delete(Path.Combine(directory.FullName, "mode.json"));
            directory.Delete();
        }
    }

    [TestMethod]
    public void LoadsModeFilesWithUppercaseWindowsExtensions()
    {
        var directory = Directory.CreateTempSubdirectory();
        var textPath = Path.Combine(directory.FullName, "fixture.TXT");
        var jsonPath = Path.Combine(directory.FullName, "fixture.JSON");
        try
        {
            File.WriteAllLines(textPath, ["# Legacy fixture,0", @"fixture\.exe", @"!bypass\.exe"]);
            var mode = (Redirector)ModeHelper.LoadMode(textPath);
            CollectionAssert.AreEqual(new[] { @"fixture\.exe" }, mode.Handle);
            CollectionAssert.AreEqual(new[] { @"bypass\.exe" }, mode.Bypass);

            mode.FullName = jsonPath;
            mode.WriteFile();
            var reloaded = (Redirector)ModeHelper.LoadMode(jsonPath);
            CollectionAssert.AreEqual(mode.Handle, reloaded.Handle);
            CollectionAssert.AreEqual(mode.Bypass, reloaded.Bypass);

            File.WriteAllText(textPath, string.Empty);
            Assert.ThrowsExactly<FormatException>(() => ModeHelper.LoadMode(textPath));
        }
        finally
        {
            File.Delete(textPath);
            File.Delete(jsonPath);
            directory.Delete();
        }
    }

    [TestMethod]
    public void HttpNodeShareLinkRoundTripsConnectionSettings()
    {
        var original = new HttpServer("127.0.0.1", 49002, "sample-user", "sample-password") { Remarks = "HTTP fixture" };
        original.ProtoExtra.HttpHeaders = "X-Sample: Round trip";

        var link = ShareLink.GetShareLink(original);
        StringAssert.StartsWith(link, "Netch://");
        var imported = (HttpServer)ShareLink.ParseText(link).Single();
        Assert.AreEqual(original.Address, imported.Address);
        Assert.AreEqual(original.Port, imported.Port);
        Assert.AreEqual(original.Remarks, imported.Remarks);
        Assert.AreEqual(original.Username, imported.Username);
        Assert.AreEqual(original.Password, imported.Password);
        Assert.AreEqual(original.ProtoExtra.HttpHeaders, imported.ProtoExtra.HttpHeaders);
    }

    [TestMethod]
    public void Ipv4OnlyTunIgnoresStoredIpv6RoutesBeforeCallingWindows()
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var controller = new TUNController();
        typeof(TUNController).GetField("_tunConfig", flags)!.SetValue(controller, new TUNConfig { EnableIPv6 = false });
        var ensureRoutes = typeof(TUNController).GetMethod("EnsureTrackedRoutes", flags)!;
        ensureRoutes.Invoke(controller, [default(NetRoute), null, new[] { "::/0", "2001:db8::/32" }]);
    }

    [TestMethod]
    public void TunUsesLongOptionsAndEscapesProxyCredentials()
    {
        var build = typeof(Netch.Interops.TUN2Socks).GetMethod("BuildArgs", BindingFlags.Static | BindingFlags.NonPublic)!;
        var args = (string[])build.Invoke(null, ["Netch", "[::1]", 2801, "user@name", "a:b/c"])!;
#if DEBUG
        const string expectedLogLevel = "debug";
#else
        const string expectedLogLevel = "warn";
#endif
        CollectionAssert.AreEqual(new[] { "--device", "tun://Netch", "--proxy",
            "socks5://user%40name:a%3Ab%2Fc@[::1]:2801/", "--mtu", "1500", "--loglevel", expectedLogLevel }, args);
    }

    [TestMethod]
    public void TunRejectsNoncontiguousSubnetMask()
    {
        var mode = new TunMode();
        TUNController.ValidateConfiguration(mode, new TUNConfig { Netmask = "255.255.254.0" });
        Assert.ThrowsExactly<MessageException>(() =>
            TUNController.ValidateConfiguration(mode, new TUNConfig { Netmask = "255.0.255.0" }));
    }

    [TestMethod]
    public void TextModeRejectsCircularIncludes()
    {
        var folder = Directory.CreateTempSubdirectory();
        try
        {
            var left = Path.Combine(folder.FullName, "left.txt");
            var right = Path.Combine(folder.FullName, "right.txt");
            File.WriteAllLines(left, ["# Left,0", $"#include <{right}>"]);
            File.WriteAllLines(right, ["# Right,0", $"#include <{left}>"]);
            StringAssert.Contains(Assert.ThrowsExactly<FormatException>(() => ModeHelper.LoadMode(left)).Message, "Circular mode include");
        }
        finally { folder.Delete(recursive: true); }
    }

    [TestMethod]
    public void TextModeResolvesIncludesWhenDirectoryContainsDotH()
    {
        var folder = Directory.CreateTempSubdirectory("netch.h-");
        try
        {
            var main = Path.Combine(folder.FullName, "main.txt");
            var includedTxt = Path.Combine(folder.FullName, "included.txt");
            var includedHeader = Path.Combine(folder.FullName, "included.h");
            File.WriteAllLines(includedTxt, ["# Included,0", @"included\.exe"]);
            File.WriteAllLines(main, ["# Main,0", $"#include <{includedHeader}>", @"main\.exe"]);

            var mode = (Redirector)ModeHelper.LoadMode(main);
            CollectionAssert.AreEqual(new[] { @"included\.exe", @"main\.exe" }, mode.Handle);
        }
        finally { folder.Delete(recursive: true); }
    }

    [TestMethod]
    public void MissingSceneModeSettingsKeepLegacyConnectionBehavior()
    {
        Netch.Global.Settings = new Setting
        {
            Redirector = { FilterUDP = false, FilterDNS = false, FilterICMP = true, ICMPDelay = 29, DNSHost = "1.1.1.1:5353" },
            TUNTAP = { Address = "10.0.99.2", ProxyDNS = true }
        };
        var legacy = new Redirector { FilterUDP = true, ICMPDelay = 11, DNSHost = "8.8.8.8:53" };
        var explicitMode = new Redirector { FilterICMP = true, FilterDNS = true, DNSHost = "9.9.9.9:53" };
        var tun = new TunMode();

        ModeHelper.CompleteSceneDefaults(legacy);
        ModeHelper.CompleteSceneDefaults(explicitMode);
        ModeHelper.CompleteSceneDefaults(tun);

        Assert.AreEqual(true, legacy.FilterUDP);
        Assert.AreEqual(false, legacy.FilterDNS);
        Assert.AreEqual(29, legacy.ICMPDelay);
        Assert.AreEqual("1.1.1.1:5353", legacy.DNSHost);
        Assert.AreEqual(10, explicitMode.ICMPDelay);
        Assert.AreEqual("9.9.9.9:53", explicitMode.DNSHost);
        Assert.AreEqual("10.0.99.2", tun.Network!.Address);
        Assert.IsTrue(tun.Network.ProxyDNS);
    }

    [TestMethod]
    public void SubscriptionUpdateKeepsNodeIds()
    {
        SocksServer Node(string remarks, string address, int port = 443) => new() { Remarks = remarks, Address = address, Port = port };

        var renamed = Node("HK 01", "hk.example");
        var twinA = Node("CDN A", "cdn.example");
        var twinB = Node("CDN B", "cdn.example");
        var dropped = Node("JP 01", "jp.example");
        var previous = new List<Server> { renamed, twinA, twinB, dropped };

        var update = new List<Server>
        {
            Node("CDN B", "cdn.example"),
            Node("Hong Kong 01", "HK.example"),
            Node("CDN A", "cdn.example"),
            Node("SG 01", "sg.example")
        };

        SubscriptionUtil.InheritIds(previous, update);

        Assert.AreEqual(twinB.Id, update[0].Id, "same endpoint twice: matched by name");
        Assert.AreEqual(renamed.Id, update[1].Id, "renamed node: matched by protocol, address and port");
        Assert.AreEqual(twinA.Id, update[2].Id);
        Assert.IsFalse(new[] { renamed.Id, twinA.Id, twinB.Id, dropped.Id }.Contains(update[3].Id), "new node keeps a new id");
    }

    [TestMethod]
    public void SceneIconLegacyEmojiMigrationMapsAllSixteenIcons()
    {
        var legacy = new[]
        {
            ("🎮", SceneIcon.Gamepad),
            ("🌐", SceneIcon.Globe),
            ("🧭", SceneIcon.Compass),
            ("🕹", SceneIcon.Console),
            ("🕹️", SceneIcon.Console),
            ("🛠", SceneIcon.Tools),
            ("🛠️", SceneIcon.Tools),
            ("💬", SceneIcon.Chat),
            ("📺", SceneIcon.Video),
            ("🎵", SceneIcon.Music),
            ("💼", SceneIcon.Work),
            ("🚀", SceneIcon.Speed),
            ("⚡", SceneIcon.Flash),
            ("🔒", SceneIcon.Lock),
            ("🏠", SceneIcon.Home),
            ("🖥", SceneIcon.Desktop),
            ("🖥️", SceneIcon.Desktop),
            ("📱", SceneIcon.Mobile),
            ("☁", SceneIcon.Cloud),
            ("☁️", SceneIcon.Cloud)
        };

        Assert.AreEqual(16, SceneIcon.Presets.Count);
        foreach (var (emoji, expectedKey) in legacy)
        {
            Assert.AreEqual(expectedKey, SceneIcon.NormalizeKey(emoji));
            var glyph = SceneIcon.ToGlyph(emoji);
            Assert.IsFalse(string.IsNullOrEmpty(glyph));
            Assert.AreEqual(expectedKey, SceneIcon.NormalizeKey(glyph));
        }

        Assert.AreEqual(SceneIcon.Default, SceneIcon.NormalizeKey(""));
        Assert.AreEqual(SceneIcon.Default, SceneIcon.NormalizeKey("unknown-icon"));

        Netch.Global.Settings = new Setting
        {
            Scenes = [new Scene { Name = "Legacy", Icon = "🚀" }]
        };
        Assert.IsFalse(SceneService.Initialize());
        Assert.AreEqual(SceneIcon.Speed, SceneService.Scenes[0].Icon);
    }

    [TestMethod]
    public async Task MainControllerEnsureUdpCompatibilityBlocksUnsupportedNodesOnUdpModes()
    {
        var ssh = new SSHServer { Address = "127.0.0.1", Port = 22, Username = "root", Password = "pw" };
        var http = new HttpServer("127.0.0.1", 8080);
        var naive = new NaiveServer { Address = "127.0.0.1", Port = 443, Password = "pw" };
        var stlsNoUot = new ShadowTLSServer { Address = "127.0.0.1", Port = 443, Password = "pw", ProtoExtra = { ShadowTlsPassword = "hpw", Uot = false } };
        var stlsUot = new ShadowTLSServer { Address = "127.0.0.1", Port = 443, Password = "pw", ProtoExtra = { ShadowTlsPassword = "hpw", Uot = true } };

        var tunMode = new TunMode();
        var shareMode = new ShareMode();
        var udpProcessMode = new Redirector { FilterUDP = true, FilterDNS = false };
        var proxiedDnsProcessMode = new Redirector { FilterUDP = false, FilterDNS = true, DNSProxy = true };
        var tcpOnlyProcessMode = new Redirector { FilterUDP = false, FilterDNS = false };
        var directDnsProcessMode = new Redirector { FilterUDP = false, FilterDNS = true, DNSProxy = false };

        foreach (var unsupported in new Server[] { ssh, http, stlsNoUot, naive })
        {
            Assert.ThrowsExactly<MessageException>(() => MainController.EnsureUdpCompatibility(unsupported, tunMode));
            Assert.ThrowsExactly<MessageException>(() => MainController.EnsureUdpCompatibility(unsupported, shareMode));
            Assert.ThrowsExactly<MessageException>(() => MainController.EnsureUdpCompatibility(unsupported, udpProcessMode));
            Assert.ThrowsExactly<MessageException>(() => MainController.EnsureUdpCompatibility(unsupported, proxiedDnsProcessMode));

            MainController.EnsureUdpCompatibility(unsupported, tcpOnlyProcessMode);
            MainController.EnsureUdpCompatibility(unsupported, directDnsProcessMode);
            MainController.EnsureUdpCompatibility(unsupported, null);
        }

        MainController.EnsureUdpCompatibility(stlsUot, tunMode);
        MainController.EnsureUdpCompatibility(stlsUot, udpProcessMode);

        await Assert.ThrowsExactlyAsync<MessageException>(() => MainController.StartAsync(ssh, tunMode));
        Assert.IsNull(MainController.Server);
        Assert.IsNull(MainController.Mode);
    }

    [TestMethod]
    public async Task MainControllerStopOrdersModeBeforeServerAndClearsState()
    {
        var log = new List<string>();
        var fakeMode = new FakeModeController(log);
        var fakeServer = new FakeServerController(log);

        SetMainControllerProperty("ModeController", fakeMode);
        SetMainControllerProperty("ServerController", fakeServer);
        SetMainControllerProperty("Socks5Server", new SocksServer("127.0.0.1", 2801));
        SetMainControllerProperty("Server", new SocksServer("127.0.0.1", 1080));
        SetMainControllerProperty("Mode", new TunMode());

        await MainController.StopAsync();

        CollectionAssert.AreEqual(new[] { "mode.stop", "server.stop" }, log);
        Assert.IsNull(MainController.ModeController);
        Assert.IsNull(MainController.ServerController);
        Assert.IsNull(MainController.Socks5Server);
        Assert.IsNull(MainController.Server);
        Assert.IsNull(MainController.Mode);
    }

    [TestMethod]
    public async Task DnsUtilsClearCachePreventsStaleInFlightWriteback()
    {
        DnsUtils.ClearCache();
        using var udp = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
        var dnsPort = ((IPEndPoint)udp.Client.LocalEndPoint!).Port;
        using var queryReceived = new SemaphoreSlim(0, 1);
        using var allowReply = new SemaphoreSlim(0, 1);

        var serverTask = Task.Run(async () =>
        {
            var req = await udp.ReceiveAsync();
            queryReceived.Release();
            await allowReply.WaitAsync();

            // Minimal RFC 1035 DNS A response (10.20.30.40) echoing transaction ID + question.
            var pos = 12;
            while (pos < req.Buffer.Length && req.Buffer[pos] != 0)
            {
                if ((req.Buffer[pos] & 0xC0) == 0xC0)
                {
                    pos += 2;
                    break;
                }
                pos += 1 + req.Buffer[pos];
            }
            if (pos < req.Buffer.Length && req.Buffer[pos] == 0)
                pos++;
            var questionEnd = Math.Min(req.Buffer.Length, pos + 4);

            var resp = new byte[questionEnd + 16];
            Buffer.BlockCopy(req.Buffer, 0, resp, 0, questionEnd);
            resp[2] = 0x81;
            resp[3] = 0x80;
            resp[6] = 0x00;
            resp[7] = 0x01;
            resp[8] = 0x00;
            resp[9] = 0x00;
            resp[10] = 0x00;
            resp[11] = 0x00;
            resp[questionEnd + 0] = 0xC0;
            resp[questionEnd + 1] = 0x0C;
            resp[questionEnd + 2] = 0x00;
            resp[questionEnd + 3] = 0x01;
            resp[questionEnd + 4] = 0x00;
            resp[questionEnd + 5] = 0x01;
            resp[questionEnd + 6] = 0x00;
            resp[questionEnd + 7] = 0x00;
            resp[questionEnd + 8] = 0x00;
            resp[questionEnd + 9] = 0x3C;
            resp[questionEnd + 10] = 0x00;
            resp[questionEnd + 11] = 0x04;
            resp[questionEnd + 12] = 10;
            resp[questionEnd + 13] = 20;
            resp[questionEnd + 14] = 30;
            resp[questionEnd + 15] = 40;
            await udp.SendAsync(resp, req.RemoteEndPoint);
        });

        var lookupTask = DnsUtils.LookupAllAsync(
            "race.test.invalid",
            AddressFamily.InterNetwork,
            timeout: 3000,
            dns: $"127.0.0.1:{dnsPort}",
            useCache: true);

        Assert.IsTrue(await queryReceived.WaitAsync(TimeSpan.FromSeconds(5)));
        DnsUtils.ClearCache();
        allowReply.Release();

        var result = await lookupTask;
        await serverTask;

        Assert.AreEqual(1, result.Length);
        Assert.AreEqual(IPAddress.Parse("10.20.30.40"), result[0]);

        var cacheField = typeof(DnsUtils).GetField("Cache", BindingFlags.Static | BindingFlags.NonPublic)!;
        var cacheDict = cacheField.GetValue(null)!;
        var containsKey = (bool)cacheDict.GetType().GetMethod("ContainsKey")!.Invoke(cacheDict, ["race.test.invalid"])!;
        Assert.IsFalse(containsKey, "Stale in-flight DNS lookup must not write back after ClearCache().");
    }

    [TestMethod]
    public void ConfigurationCheckSettingNormalizesGroupsAndClearsOrphanedSubscriptionFlags()
    {
        var validSub = new Subscription { Remark = " SubA ", Link = "https://example.com/sub" };
        var matchedNode = new SocksServer("a.example", 1080) { Group = " SubA ", IsSub = true };
        var orphanedSubNode = new SocksServer("b.example", 1080) { Group = " MissingSub ", IsSub = true };
        var manualWithSubFlag = new SocksServer("c.example", 1080) { Group = " NONE ", IsSub = true };
        var importedNode = new SocksServer("d.example", 1080) { Group = " ImportedGroup ", IsSub = false };
        var settings = new Setting
        {
            Subscription = [validSub],
            Server = [matchedNode, orphanedSubNode, manualWithSubFlag, importedNode]
        };

        var checkSetting = typeof(Configuration).GetMethod("CheckSetting", BindingFlags.Static | BindingFlags.NonPublic)!;
        checkSetting.Invoke(null, [settings]);

        Assert.AreEqual("SubA", validSub.Remark);
        Assert.AreEqual("SubA", matchedNode.Group);
        Assert.IsTrue(matchedNode.IsSub);

        Assert.AreEqual("MissingSub", orphanedSubNode.Group);
        Assert.IsFalse(orphanedSubNode.IsSub);

        Assert.AreEqual(Netch.Constants.DefaultGroup, manualWithSubFlag.Group);
        Assert.IsFalse(manualWithSubFlag.IsSub);

        Assert.AreEqual("ImportedGroup", importedNode.Group);
        Assert.IsFalse(importedNode.IsSub);
    }

    private static void SetMainControllerProperty(string name, object value)
    {
        var prop = typeof(MainController).GetProperty(name, BindingFlags.Public | BindingFlags.Static)!;
        prop.SetValue(null, value);
    }

    private sealed class FakeServerController(List<string> log) : IServerController
    {
        public string Name => "FakeServer";
        public ushort? Socks5LocalPort { get; set; } = 2801;
        public string LocalAddress { get; set; } = "127.0.0.1";
        public Task<SocksServer> StartAsync(Server s) => Task.FromResult(new SocksServer("127.0.0.1", 2801));
        public Task StopAsync()
        {
            log.Add("server.stop");
            return Task.CompletedTask;
        }
    }

    private sealed class FakeModeController(List<string> log) : IModeController
    {
        public string Name => "FakeMode";
        public ModeFeature Features => ModeFeature.SupportSocks5Auth;
        public Task StartAsync(SocksServer s, Mode mode) => Task.CompletedTask;
        public Task StopAsync()
        {
            log.Add("mode.stop");
            return Task.CompletedTask;
        }
    }
}
