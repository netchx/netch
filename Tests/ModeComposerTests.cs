#nullable enable
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Netch.Models;
using Netch.Models.Modes;
using Netch.Models.Modes.ProcessMode;
using Netch.Models.Modes.ShareMode;
using Netch.Models.Modes.TunMode;
using Netch.Models.Modes.WebProxyMode;
using Netch.Services;

namespace Tests;

[TestClass]
[DoNotParallelize]
public class ModeComposerTests
{
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
        Netch.Global.Settings = _savedSettings;
    }

    [TestMethod]
    public void CanAddModeAndValidateSelectionEnforceSchemeARulesWithExclusiveTun()
    {
        var proc1 = new Redirector { FullName = "proc1.json" };
        var proc2 = new Redirector { FullName = "proc2.json" };
        var tun1 = new TunMode { FullName = "tun1.json" };
        var tun2 = new TunMode { FullName = "tun2.json" };
        var web1 = new WebProxyMode { FullName = "web1.json" };
        var web2 = new WebProxyMode { FullName = "web2.json" };
        var share1 = new ShareMode { FullName = "share1.json" };
        var share2 = new ShareMode { FullName = "share2.json" };

        Assert.IsTrue(ModeComposer.CanAddMode([proc1], proc2, out _));
        Assert.IsTrue(ModeComposer.CanAddMode([proc1], web1, out _));
        Assert.IsTrue(ModeComposer.CanAddMode([proc1, web1], share1, out _));
        Assert.IsTrue(ModeComposer.CanAddMode([tun1], tun2, out _));

        Assert.IsFalse(ModeComposer.CanAddMode([tun1], proc1, out var tunProcReason));
        Assert.IsFalse(string.IsNullOrEmpty(tunProcReason));
        Assert.IsFalse(ModeComposer.CanAddMode([tun1], web1, out _));
        Assert.IsFalse(ModeComposer.CanAddMode([tun1], share1, out _));
        Assert.IsFalse(ModeComposer.CanAddMode([proc1], tun1, out _));
        Assert.IsFalse(ModeComposer.CanAddMode([web1], web2, out _));
        Assert.IsFalse(ModeComposer.CanAddMode([share1], share2, out _));

        Assert.IsFalse(ModeComposer.ValidateSelection([], out _));
        Assert.IsFalse(ModeComposer.ValidateSelection([tun1, proc1], out _));
        Assert.IsFalse(ModeComposer.ValidateSelection([tun1, web1], out _));
        Assert.IsFalse(ModeComposer.ValidateSelection([tun1, share1], out _));
        Assert.IsFalse(ModeComposer.ValidateSelection([web1, web2], out _));
        Assert.IsFalse(ModeComposer.ValidateSelection([share1, share2], out _));

        Assert.IsTrue(ModeComposer.ValidateSelection([proc1, proc2, web1, share1], out _));
        Assert.IsTrue(ModeComposer.ValidateSelection([tun1, tun2], out _));
    }

    [TestMethod]
    public void ComposeSingleProcessModeReturnsOriginalInstanceUnlessLoopbackMustBeDisabled()
    {
        var single = new Redirector
        {
            FullName = @"Game\Steam.json",
            FilterLoopback = false,
            Handle = [@"steam\.exe$"]
        };
        var sameResult = ModeComposer.Compose([single]);
        Assert.AreSame(single, sameResult.EffectiveProcessMode);
        Assert.IsEmpty(sameResult.Warnings);

        var loopbackProcess = new Redirector
        {
            FullName = @"Game\Loop.json",
            FilterLoopback = true,
            Handle = [@"game\.exe$"]
        };
        var web = new WebProxyMode { FullName = @"Web\PAC.json" };
        var concurrent = ModeComposer.Compose([loopbackProcess, web]);
        Assert.AreNotSame(loopbackProcess, concurrent.EffectiveProcessMode);
        Assert.AreEqual(@"Game\Loop.json", concurrent.EffectiveProcessMode!.FullName);
        Assert.IsFalse(concurrent.EffectiveProcessMode.FilterLoopback);
        Assert.HasCount(1, concurrent.Warnings);
        Assert.HasCount(2, concurrent.EffectiveModes);
        Assert.AreEqual(ModeType.ProcessMode, concurrent.EffectiveModes[0].Type);
        Assert.AreEqual(ModeType.WebProxyMode, concurrent.EffectiveModes[1].Type);
    }

    [TestMethod]
    public void ComposeMultipleProcessModesMergesRulesInOrderAndGuardsHandleIpTrap()
    {
        Netch.Global.Settings.Redirector.DNSHost = "1.1.1.1:53";
        Netch.Global.Settings.Redirector.ICMPDelay = 10;

        var game = new Redirector
        {
            FullName = "game.json",
            Remark = { ["en"] = "Game" },
            FilterTCP = true,
            FilterUDP = false,
            FilterICMP = false,
            FilterDNS = true,
            DNSHost = "1.1.1.1:53",
            HandleOnlyDNS = true,
            FilterIntranet = false,
            FilterLoopback = true,
            Handle = [@"game\.exe$", @"helper\.exe$"],
            Bypass = [@"anticheat\.exe$"],
            HandleIPs = ["198.51.100.0/24"],
            BypassIPs = ["198.51.100.99/32"]
        };

        var overlay = new Redirector
        {
            FullName = "overlay.json",
            Remark = { ["en"] = "Overlay" },
            FilterTCP = true,
            FilterUDP = true,
            FilterICMP = true,
            ICMPDelay = 25,
            FilterDNS = true,
            DNSHost = "8.8.8.8:53",
            HandleOnlyDNS = false,
            FilterIntranet = true,
            FilterLoopback = false,
            Handle = [@"anticheat\.exe$", @"extra\.exe$"],
            Bypass = [@"helper\.exe$"],
            HandleIPs = ["203.0.113.0/24"],
            BypassIPs = ["10.1.2.3"]
        };

        var web = new WebProxyMode { FullName = "web.json" };
        var share = new ShareMode { FullName = "share.json" };

        var result = ModeComposer.Compose([share, game, web, overlay]);

        Assert.AreEqual(3, result.EffectiveModes.Count);
        Assert.AreEqual(ModeType.ProcessMode, result.EffectiveModes[0].Type);
        Assert.AreEqual(ModeType.WebProxyMode, result.EffectiveModes[1].Type);
        Assert.AreEqual(ModeType.ShareMode, result.EffectiveModes[2].Type);

        var merged = result.EffectiveProcessMode!;
        Assert.AreEqual(string.Empty, merged.FullName);
        CollectionAssert.AreEqual(new[] { @"game\.exe$", @"anticheat\.exe$", @"extra\.exe$" }, merged.Handle);
        CollectionAssert.AreEqual(new[] { @"helper\.exe$" }, merged.Bypass);
        CollectionAssert.AreEqual(new[] { "198.51.100.0/24", "203.0.113.0/24" }, merged.HandleIPs);
        CollectionAssert.AreEqual(new[] { "198.51.100.99/32", "10.1.2.3/32" }, merged.BypassIPs);

        Assert.IsTrue(merged.FilterTCP);
        Assert.IsTrue(merged.FilterUDP);
        Assert.IsTrue(merged.FilterICMP);
        Assert.AreEqual(25, merged.ICMPDelay);
        Assert.IsTrue(merged.FilterDNS);
        Assert.AreEqual("8.8.8.8:53", merged.DNSHost);
        Assert.IsFalse(merged.HandleOnlyDNS);
        Assert.IsTrue(merged.FilterIntranet);
        Assert.IsFalse(merged.FilterLoopback);
        Assert.IsGreaterThanOrEqualTo(2, result.Warnings.Count);
    }

    [TestMethod]
    public void ComposeProcessModesRelaxesHandleIpsOnlyWhenAnotherProcessHandlerIsUnrestricted()
    {
        var ipRestrictedGame = new Redirector
        {
            FullName = "restricted.json",
            Handle = [@"restricted\.exe$"],
            HandleIPs = ["198.51.100.0/24"]
        };
        var ipBypassOnlyOverlay = new Redirector
        {
            FullName = "ip-overlay.json",
            Handle = [],
            BypassIPs = ["198.51.100.10"]
        };

        var withPureIpOverlay = ModeComposer.Compose([ipRestrictedGame, ipBypassOnlyOverlay]);
        CollectionAssert.AreEqual(new[] { "198.51.100.0/24" }, withPureIpOverlay.EffectiveProcessMode!.HandleIPs);
        CollectionAssert.AreEqual(new[] { "198.51.100.10/32" }, withPureIpOverlay.EffectiveProcessMode.BypassIPs);
        Assert.IsEmpty(withPureIpOverlay.Warnings);

        var unrestrictedMode = new Redirector
        {
            FullName = "steam.json",
            Handle = [@"steam\.exe$"],
            HandleIPs = []
        };

        var withUnrestricted = ModeComposer.Compose([ipRestrictedGame, unrestrictedMode]);
        Assert.IsEmpty(withUnrestricted.EffectiveProcessMode!.HandleIPs);
        Assert.HasCount(1, withUnrestricted.Warnings);
    }

    [TestMethod]
    public void ComposeMultipleTunModesMergesRoutesResolvesExactCollisionsAndDoesNotMutateGlobalDefaults()
    {
        Netch.Global.Settings.TUNTAP.Address = "10.0.236.10";
        Netch.Global.Settings.TUNTAP.BypassIPs = ["192.0.2.1/32"];

        var tun1 = new TunMode
        {
            FullName = "tun1.json",
            Remark = { ["en"] = "TUN 1" },
            Network = new TUNConfig
            {
                Address = "10.0.10.10",
                Gateway = "10.0.10.1",
                BypassIPs = ["1.1.1.1/32"]
            },
            Handle = ["198.51.100.0/24", "203.0.113.0/24"],
            Bypass = ["10.0.0.0/8"]
        };

        var tun2 = new TunMode
        {
            FullName = "tun2.json",
            Remark = { ["en"] = "TUN 2" },
            Network = new TUNConfig
            {
                Address = "10.0.20.10",
                Gateway = "10.0.20.1",
                BypassIPs = ["8.8.8.8/32"]
            },
            Handle = ["198.51.100.0/24", "2001:db8::/32"],
            Bypass = ["203.0.113.0/24"]
        };

        var result = ModeComposer.Compose([tun1, tun2]);
        var merged = result.EffectiveTunMode!;

        Assert.AreEqual(string.Empty, merged.FullName);
        Assert.AreEqual("10.0.10.10", merged.Network!.Address);
        CollectionAssert.AreEqual(new[] { "1.1.1.1/32", "8.8.8.8/32" }, merged.Network.BypassIPs);
        CollectionAssert.AreEqual(new[] { "198.51.100.0/24", "2001:db8::/32" }, merged.Handle);
        CollectionAssert.AreEqual(new[] { "10.0.0.0/8", "203.0.113.0/24" }, merged.Bypass);
        Assert.HasCount(2, result.Warnings);
        CollectionAssert.AreEqual(new[] { "192.0.2.1/32" }, Netch.Global.Settings.TUNTAP.BypassIPs);
    }

    [TestMethod]
    public void DeduplicationAndInvalidSelectionGuardrails()
    {
        var proc1 = new Redirector { FullName = "proc1.json" };
        var tun1 = new TunMode { FullName = @"TUNTAP\Tun1.json" };
        var tun1AltCase = new TunMode { FullName = @"tuntap\tun1.json" };
        var web1 = new WebProxyMode { FullName = @"Web\Pac.json" };
        var web1AltCase = new WebProxyMode { FullName = @"web\pac.json" };
        var share1 = new ShareMode { FullName = @"Share\Lan.json" };
        var share1AltCase = new ShareMode { FullName = @"share\lan.json" };

        Assert.IsTrue(ModeComposer.CanAddMode([web1], web1AltCase, out var webReason));
        Assert.IsNull(webReason);
        Assert.IsTrue(ModeComposer.CanAddMode([share1], share1AltCase, out var shareReason));
        Assert.IsNull(shareReason);
        Assert.IsTrue(ModeComposer.CanAddMode([tun1], tun1, out var tunRefReason));
        Assert.IsNull(tunRefReason);
        Assert.IsTrue(ModeComposer.CanAddMode([tun1], tun1AltCase, out var tunPathReason));
        Assert.IsNull(tunPathReason);

        Assert.IsFalse(ModeComposer.ValidateSelection(null, out _));
        Assert.IsTrue(ModeComposer.ValidateSelection([web1, web1AltCase], out _));

        var deduped = ModeComposer.Compose([web1, web1AltCase]);
        Assert.HasCount(1, deduped.SourceModes);
        Assert.HasCount(1, deduped.EffectiveModes);
        Assert.AreSame(web1, deduped.EffectiveWebProxyMode);

        Assert.ThrowsExactly<MessageException>(() => ModeComposer.Compose([]));
        Assert.ThrowsExactly<MessageException>(() => ModeComposer.Compose([tun1, proc1]));
    }

    [TestMethod]
    public void ComposeSingleTunAndConcurrentWebShareWithoutProcess()
    {
        var tun = new TunMode { FullName = @"TUNTAP\Global.json" };
        var singleTun = ModeComposer.Compose([tun]);
        Assert.AreSame(tun, singleTun.EffectiveTunMode);
        Assert.IsEmpty(singleTun.Warnings);
        Assert.HasCount(1, singleTun.EffectiveModes);

        var web = new WebProxyMode { FullName = @"Web\Pac.json" };
        var share = new ShareMode { FullName = @"Share\Lan.json" };
        var concurrent = ModeComposer.Compose([share, web]);

        Assert.IsNull(concurrent.EffectiveProcessMode);
        Assert.IsNull(concurrent.EffectiveTunMode);
        Assert.AreSame(web, concurrent.EffectiveWebProxyMode);
        Assert.AreSame(share, concurrent.EffectiveShareMode);
        CollectionAssert.AreEqual(new Mode[] { web, share }, concurrent.EffectiveModes.ToArray());
        Assert.IsEmpty(concurrent.Warnings);
    }

    [TestMethod]
    public void ComposeProcessModesPreservesLoopbackWithoutWebProxyIgnoresDisabledDnsAndOverridesIpsInOrder()
    {
        var mode1 = new Redirector
        {
            FullName = "m1.json",
            FilterDNS = true,
            DNSHost = "1.1.1.1:53",
            HandleOnlyDNS = true,
            FilterLoopback = true,
            Handle = [@"app1\.exe$"],
            HandleIPs = ["198.51.100.20/32"],
            BypassIPs = ["198.51.100.10"]
        };
        var mode2 = new Redirector
        {
            FullName = "m2.json",
            FilterDNS = false,
            DNSHost = "9.9.9.9:53",
            HandleOnlyDNS = true,
            FilterLoopback = false,
            Handle = [@"app2\.exe$"],
            HandleIPs = ["198.51.100.10/32"],
            BypassIPs = ["198.51.100.20"]
        };

        var result = ModeComposer.Compose([mode1, mode2]);
        var merged = result.EffectiveProcessMode!;

        Assert.IsTrue(merged.FilterLoopback);
        Assert.IsTrue(merged.HandleOnlyDNS);
        Assert.AreEqual("1.1.1.1:53", merged.DNSHost);
        Assert.IsEmpty(result.Warnings);
        CollectionAssert.AreEqual(new[] { "198.51.100.10/32" }, merged.HandleIPs);
        CollectionAssert.AreEqual(new[] { "198.51.100.20/32" }, merged.BypassIPs);
    }

    [TestMethod]
    public void ComposeMultipleTunModesWithMatchingNetworkEmitsNoAdapterWarningAndNormalizesCidrs()
    {
        Netch.Global.Settings.TUNTAP = new TUNConfig
        {
            Address = "10.0.236.10",
            Netmask = "255.255.255.0",
            Gateway = "10.0.236.1",
            BypassIPs = ["1.1.1.1", "1.1.1.1/32"]
        };

        var tun1 = new TunMode
        {
            FullName = "t1.json",
            Handle = ["203.0.113.5/24", "198.51.100.1"],
            Bypass = []
        };
        var tun2 = new TunMode
        {
            FullName = "t2.json",
            Handle = [],
            Bypass = ["203.0.113.0/24"]
        };

        var result = ModeComposer.Compose([tun1, tun2]);
        var merged = result.EffectiveTunMode!;

        Assert.HasCount(1, result.Warnings);
        CollectionAssert.AreEqual(new[] { "198.51.100.1/32" }, merged.Handle);
        CollectionAssert.AreEqual(new[] { "203.0.113.0/24" }, merged.Bypass);
        CollectionAssert.AreEqual(new[] { "1.1.1.1/32" }, merged.Network!.BypassIPs);
    }
}
