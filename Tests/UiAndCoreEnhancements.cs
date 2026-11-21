#nullable enable
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Netch.Controllers;
using Netch.Models;
using Netch.Servers;
using Netch.Utils;

namespace Tests;

[TestClass]
public class UiAndCoreEnhancements
{
    private sealed class TestGuard : Guard
    {
        public TestGuard(string mainFile) : base(mainFile)
        {
        }

        public override string Name => "test-guard";
    }

    [TestMethod]
    public void LogLineParserNormalizesAllCoresAndPreservesContinuationLevel()
    {
        var fallback = new DateTime(2026, 11, 20, 8, 30, 0, DateTimeKind.Local);

        var vrb = LogLineParser.Parse("[2026-11-20 12:00:00.100] [VRB] Trace detail", "netch", null, fallback);
        var dbg = LogLineParser.Parse("[2026-11-20 12:00:00.200] [DBG] Debug detail", "netch", vrb, fallback);
        var ftl = LogLineParser.Parse("[2026-11-20 12:00:00.300] [FTL] Fatal crash", "netch", dbg, fallback);
        Assert.AreEqual(0, vrb.Level);
        Assert.AreEqual(1, dbg.Level);
        Assert.AreEqual(4, ftl.Level);

        var serilog = LogLineParser.Parse("[2026-11-20 12:00:01.123] [INF] Netch started", "netch", null, fallback);
        Assert.AreEqual("2026-11-20 12:00:01.123", serilog.Time);
        Assert.AreEqual(2, serilog.Level);
        Assert.AreEqual("INFO", serilog.LevelLabel);
        Assert.AreEqual("netch", serilog.Source);
        Assert.AreEqual("Netch started", serilog.Message);
        Assert.IsTrue(serilog.ToString().StartsWith("2026-11-20 12:00:01.123  INFO", StringComparison.Ordinal));

        var continuation = LogLineParser.Parse(@"  C:\Games\ErrorReporter\error_game.exe", "netch", serilog, fallback);
        Assert.AreEqual(serilog.Time, continuation.Time);
        Assert.AreEqual(2, continuation.Level);
        Assert.AreEqual(serilog.Source, continuation.Source);

        var nf = LogLineParser.Parse("[2026-11-20 12:00:02.000] [WRN] Redirector bypass loaded", "netch", serilog, fallback);
        Assert.AreEqual(3, nf.Level);
        Assert.AreEqual("nf", nf.Source);

        var mihomo = LogLineParser.Parse(
            "time=\"2026-11-20T12:00:03+08:00\" level=warning msg=\"dial \\\"proxy\\\" failed\"",
            "mihomo", null, fallback);
        Assert.AreEqual(3, mihomo.Level);
        Assert.AreEqual("mihomo", mihomo.Source);
        Assert.AreEqual("dial \"proxy\" failed", mihomo.Message);
        Assert.IsFalse(mihomo.Message.Contains("level=", StringComparison.OrdinalIgnoreCase));

        var tun2socks = LogLineParser.Parse(
            "[tun2socks] time=\"2026-11-20T12:00:04+08:00\" level=error msg=\"tun device error\"",
            "tun2socks", null, fallback);
        Assert.AreEqual(4, tun2socks.Level);
        Assert.AreEqual("tun2socks", tun2socks.Source);
        Assert.AreEqual("tun device error", tun2socks.Message);

        var singBox = LogLineParser.Parse(
            "+0800 2026-11-20 12:00:05 \x1B[36mINFO\x1B[0m [12345 0ms] outbound/socks: connected",
            "sing-box", null, fallback);
        Assert.AreEqual(2, singBox.Level);
        Assert.AreEqual("sing-box", singBox.Source);
        Assert.AreEqual("[12345 0ms] outbound/socks: connected", singBox.Message);
        Assert.IsFalse(singBox.Message.Contains("\x1B", StringComparison.Ordinal));

        var xray = LogLineParser.Parse(
            "2026/11/20 12:00:06.123456 [Warning] app/proxyman/inbound: listener warning",
            "Xray", null, fallback);
        Assert.AreEqual("2026-11-20 12:00:06.123", xray.Time);
        Assert.AreEqual(3, xray.Level);
        Assert.AreEqual("Xray", xray.Source);
        Assert.AreEqual("app/proxyman/inbound: listener warning", xray.Message);

        var bracketTime = LogLineParser.Parse("[12:00:07][INFO] AioDNS ready", "AioDNS", null, fallback);
        Assert.AreEqual("2026-11-20 12:00:07", bracketTime.Time);
        Assert.AreEqual("12:00:07", bracketTime.DisplayTime);
        Assert.AreEqual(2, bracketTime.Level);
        Assert.AreEqual("AioDNS ready", bracketTime.Message);

        var logfmtPath = LogLineParser.Parse(
            "time=\"2026-11-20T12:00:03+08:00\" level=info msg=\"open C:\\\\temp\\\\netch\\\\tun.yaml\"",
            "mihomo", null, fallback);
        Assert.AreEqual(2, logfmtPath.Level);
        Assert.AreEqual(@"open C:\temp\netch\tun.yaml", logfmtPath.Message);

        var utcOffset = LogLineParser.Parse(
            "+0000 2026-11-20 04:00:05 INFO started",
            "sing-box", null, fallback);
        var expectedLocal = new DateTimeOffset(2026, 11, 20, 4, 0, 5, TimeSpan.Zero)
            .ToLocalTime()
            .ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        Assert.AreEqual(expectedLocal, utcOffset.Time);

        var nonLevelBracket = LogLineParser.Parse(
            "[2026-11-20 12:00:05] [dns-router] query example.com",
            "netch", null, fallback);
        Assert.AreEqual(2, nonLevelBracket.Level);
        Assert.AreEqual("[dns-router] query example.com", nonLevelBracket.Message);
    }

    [TestMethod]
    public void HelperAndCoreProcessesUseWindowlessStartInfo()
    {
        var tunInfo = Netch.Interops.TUN2Socks.BuildStartInfo("bin\\tun2socks.exe", "Netch", "127.0.0.1", 1080);
        Assert.IsFalse(tunInfo.UseShellExecute);
        Assert.IsTrue(tunInfo.CreateNoWindow);
        Assert.AreEqual(ProcessWindowStyle.Hidden, tunInfo.WindowStyle);

        var netshInfo = NetworkInterfaceUtils.BuildNetshStartInfo("interface ip set interface 1 metric=5");
        Assert.IsFalse(netshInfo.UseShellExecute);
        Assert.IsTrue(netshInfo.CreateNoWindow);
        Assert.AreEqual(ProcessWindowStyle.Hidden, netshInfo.WindowStyle);
        Assert.AreEqual(string.Empty, netshInfo.Verb);

        var tempExe = Path.Combine(Path.GetTempPath(), $"netch-guard-test-{Guid.NewGuid():N}.exe");
        File.WriteAllBytes(tempExe, []);
        TestGuard? guard = null;
        try
        {
            guard = new TestGuard(tempExe);
            var startInfo = guard.Instance.StartInfo;
            Assert.IsFalse(startInfo.UseShellExecute);
            Assert.IsTrue(startInfo.CreateNoWindow);
            Assert.AreEqual(ProcessWindowStyle.Hidden, startInfo.WindowStyle);
        }
        finally
        {
            guard?.Instance.Dispose();
            if (File.Exists(tempExe))
                File.Delete(tempExe);
        }
    }

    [TestMethod]
    public async Task DelayTestTargetsOnlySpecifiedServers()
    {
        await DelayTestHelper.PerformTestAsync([], waitFinish: true);

        using var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        var port = (ushort)((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        var unusedPort = port == ushort.MaxValue ? (ushort)(port - 1) : (ushort)(port + 1);

        var previousTcping = Netch.Global.Settings.ServerTCPing;
        var previousMode = Netch.Global.Settings.DelayTestMode;
        Netch.Global.Settings.ServerTCPing = true;
        Netch.Global.Settings.DelayTestMode = Netch.Enums.DelayTestMode.TCPing;
        try
        {
            var target = new SocksServer("127.0.0.1", port);
            var untouched = new SocksServer("127.0.0.1", unusedPort);
            await DelayTestHelper.PerformTestAsync([target], waitFinish: true);

            Assert.IsTrue(target.Delay >= 0, $"Expected tested server delay >= 0, actual {target.Delay}");
            Assert.AreEqual(-1, untouched.Delay);
        }
        finally
        {
            Netch.Global.Settings.ServerTCPing = previousTcping;
            Netch.Global.Settings.DelayTestMode = previousMode;
        }
    }
}
