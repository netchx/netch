#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Netch.Models;
using Netch.Models.Modes.ProcessMode;
using Netch.Services;
using Netch.Utils;

namespace Tests;

[TestClass]
public class ProcessConnectionAndIpFilterTests
{
    [TestMethod]
    public void RouteUtilsNormalizesIpAndCidrCorrectly()
    {
        Assert.IsTrue(RouteUtils.TryNormalizeIpOrCidr("1.2.3.4", out var single4));
        Assert.AreEqual("1.2.3.4/32", single4);

        Assert.IsTrue(RouteUtils.TryNormalizeIpOrCidr(" 192.168.1.42/24 ", out var net4));
        Assert.AreEqual("192.168.1.0/24", net4);

        Assert.IsTrue(RouteUtils.TryNormalizeIpOrCidr("[::ffff:8.8.8.8]", out var mapped));
        Assert.AreEqual("8.8.8.8/32", mapped);

        Assert.IsTrue(RouteUtils.TryNormalizeIpOrCidr("2001:db8::42/32", out var net6));
        Assert.AreEqual("2001:db8::/32", net6);

        Assert.IsFalse(RouteUtils.TryNormalizeIpOrCidr("", out _));
        Assert.IsFalse(RouteUtils.TryNormalizeIpOrCidr("999.0.0.1", out _));
        Assert.IsFalse(RouteUtils.TryNormalizeIpOrCidr("1.2.3.4/33", out _));
        Assert.IsFalse(RouteUtils.TryNormalizeIpOrCidr("1.2.3.4/24/1", out _));

        Assert.IsTrue(RouteUtils.TryToSubnet24("142.250.80.14", out var sub24));
        Assert.AreEqual("142.250.80.0/24", sub24);

        Assert.IsTrue(RouteUtils.TryToSubnet24("2001:4860:4860::8888", out var sub64));
        Assert.AreEqual("2001:4860:4860::/64", sub64);
    }

    [TestMethod]
    public void ProcessModeJsonRoundTripAndLegacyDefaultsWork()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"netch-mode-{Guid.NewGuid():N}.json");
        try
        {
            var mode = new Redirector
            {
                FullName = tempFile,
                Handle = ["game\\.exe$"],
                Bypass = ["launcher\\.exe$"],
                HandleIPs = ["198.51.100.0/24"],
                BypassIPs = ["198.51.100.99/32"]
            };
            mode.WriteFile();

            var loaded = (Redirector)ModeHelper.LoadMode(tempFile);
            CollectionAssert.AreEqual(new[] { "game\\.exe$" }, loaded.Handle);
            CollectionAssert.AreEqual(new[] { "launcher\\.exe$" }, loaded.Bypass);
            CollectionAssert.AreEqual(new[] { "198.51.100.0/24" }, loaded.HandleIPs);
            CollectionAssert.AreEqual(new[] { "198.51.100.99/32" }, loaded.BypassIPs);

            var legacyJson = """{"type":0,"remark":{"en":"Legacy"},"filterTCP":true,"handle":["app\\.exe$"],"bypass":[]}""";
            File.WriteAllText(tempFile, legacyJson);
            var legacyLoaded = (Redirector)ModeHelper.LoadMode(tempFile);
            Assert.IsNotNull(legacyLoaded.HandleIPs);
            Assert.IsNotNull(legacyLoaded.BypassIPs);
            Assert.IsEmpty(legacyLoaded.HandleIPs);
            Assert.IsEmpty(legacyLoaded.BypassIPs);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [TestMethod]
    public void HotRuleOverlayAppliesDedupsAndReplacesOppositeKind()
    {
        var baseMode = new Redirector
        {
            Handle = ["game\\.exe$"],
            Bypass = ["helper\\.exe$"],
            HandleIPs = ["10.0.0.0/8"],
            BypassIPs = []
        };

        var overlay = new HotRuleOverlay();
        Assert.IsFalse(overlay.HasChanges);
        Assert.IsTrue(overlay.Add(HotRuleKind.BypassIp, "10.0.1.5/32"));
        Assert.IsFalse(overlay.Add(HotRuleKind.BypassIp, "10.0.1.5/32"));
        Assert.IsTrue(overlay.Add(HotRuleKind.HandleIp, "10.0.1.5/32"));
        Assert.AreEqual(1, overlay.Count);
        Assert.AreEqual(HotRuleKind.HandleIp, overlay.Entries[0].Kind);

        Assert.IsTrue(overlay.Add(HotRuleKind.HandleProcess, "helper\\.exe$"));
        var (handle, bypass, handleIps, bypassIps) = overlay.BuildEffectiveRules(baseMode);
        CollectionAssert.Contains(handle, "helper\\.exe$");
        CollectionAssert.DoesNotContain(bypass, "helper\\.exe$");
        CollectionAssert.Contains(handleIps, "10.0.1.5/32");
        Assert.IsEmpty(bypassIps);

        overlay.ApplyToMode(baseMode);
        CollectionAssert.Contains(baseMode.Handle, "helper\\.exe$");
        CollectionAssert.Contains(baseMode.HandleIPs, "10.0.1.5/32");
    }

    [TestMethod]
    public async Task ConnectionTrackerServiceSpeedFilteringAndLogFileWork()
    {
        var tempLog = Path.Combine(Path.GetTempPath(), $"netch-connlog-{Guid.NewGuid():N}.log");
        try
        {
            var tracker = new ConnectionTrackerService(tempLog);
            var dump1 = JsonSerializer.Deserialize<ProcessConnectionsDump>("""
                {
                  "active": [
                    { "id": 10, "protocol": "TCP", "state": "Active", "pid": 4242, "processName": "game.exe", "processPath": "C:\\Games\\game.exe", "remoteIp": "1.1.1.1", "remotePort": 443, "upBytes": 1000, "downBytes": 2000, "matchedRule": "game\\.exe$", "ruleSource": "Process.Handle" }
                  ],
                  "closed": []
                }
                """)!;
            tracker.ApplyConnectionSnapshot(dump1, 1.0);
            Assert.AreEqual(1000UL, tracker.ActiveConnections[0].UpSpeedBps);
            Assert.AreEqual(2000UL, tracker.ActiveConnections[0].DownSpeedBps);

            var dump2 = JsonSerializer.Deserialize<ProcessConnectionsDump>("""
                {
                  "active": [
                    { "id": 10, "protocol": "TCP", "state": "Active", "pid": 4242, "processName": "game.exe", "processPath": "C:\\Games\\game.exe", "remoteIp": "1.1.1.1", "remotePort": 443, "upBytes": 2500, "downBytes": 5000, "matchedRule": "game\\.exe$", "ruleSource": "Process.Handle" }
                  ],
                  "closed": []
                }
                """)!;
            tracker.ApplyConnectionSnapshot(dump2, 0.5);
            Assert.AreEqual(3000UL, tracker.ActiveConnections[0].UpSpeedBps);
            Assert.AreEqual(6000UL, tracker.ActiveConnections[0].DownSpeedBps);

            var byProc = ConnectionTrackerService.FilterConnections(tracker.ActiveConnections, "game", "TCP");
            Assert.HasCount(1, byProc);
            Assert.IsEmpty(ConnectionTrackerService.FilterConnections(tracker.ActiveConnections, "game", "UDP"));

            var batch = JsonSerializer.Deserialize<ProcessConnectionEventBatch>("""
                {
                  "nextSeq": 3,
                  "hasMore": false,
                  "events": [
                    { "seq": 1, "timestampMs": 1700000000000, "connectionId": 10, "action": "BYPASS_IP", "protocol": "UDP", "pid": 4242, "processName": "game.exe", "remoteIp": "192.168.1.5", "remotePort": 7777, "matchedRule": "192.168.1.0/24", "ruleSource": "IP.Bypass" },
                    { "seq": 2, "timestampMs": 1700000001000, "connectionId": 10, "action": "BYPASS_IP", "protocol": "UDP", "pid": 4242, "processName": "game.exe", "remoteIp": "192.168.1.5", "remotePort": 7777, "matchedRule": "192.168.1.0/24", "ruleSource": "IP.Bypass" },
                    { "seq": 3, "timestampMs": 1700000002000, "connectionId": 11, "action": "PROXY", "protocol": "TCP", "pid": 4242, "processName": "game.exe", "remoteIp": "1.1.1.1", "remotePort": 443, "matchedRule": "game\\.exe$", "ruleSource": "Process.Handle" }
                  ]
                }
                """)!;

            tracker.IngestEvents(batch, writeToFileLog: true);
            await tracker.FlushAndStopLogWriterAsync();

            var rawRows = ConnectionTrackerService.FilterEvents(tracker.Events, null, false, "ALL", false, out var err1);
            Assert.IsNull(err1);
            Assert.HasCount(3, rawRows);

            var dedupRows = ConnectionTrackerService.FilterEvents(tracker.Events, null, false, "ALL", true, out _);
            Assert.HasCount(2, dedupRows);
            Assert.AreEqual(2, dedupRows[0].Count);
            Assert.AreEqual(1, dedupRows[1].Count);

            var regexRows = ConnectionTrackerService.FilterEvents(tracker.Events, "1\\.1\\.1\\.1:443", true, "PROXY", false, out var err2);
            Assert.IsNull(err2);
            Assert.HasCount(1, regexRows);

            _ = ConnectionTrackerService.FilterEvents(tracker.Events, "[unclosed", true, "ALL", false, out var regexErr);
            Assert.IsNotNull(regexErr);

            Assert.IsTrue(File.Exists(tempLog));
            var logContent = await File.ReadAllTextAsync(tempLog);
            Assert.IsTrue(logContent.Contains("BYPASS_IP UDP game.exe(4242) -> 192.168.1.5:7777 [192.168.1.0/24]", StringComparison.Ordinal));
            Assert.IsTrue(logContent.Contains("PROXY TCP game.exe(4242) -> 1.1.1.1:443 [game\\.exe$]", StringComparison.Ordinal));
        }
        finally
        {
            File.Delete(tempLog);
        }
    }

    [TestMethod]
    public void EtwNetworkMonitorResolvesEndpointsFiltersNoiseAndTracksLifecycle()
    {
        var localSet = new HashSet<IPAddress>
        {
            IPAddress.Parse("192.168.1.50"),
            IPAddress.Parse("fe80::1")
        };

        var monitor = new EtwNetworkMonitor(
            pid => pid switch
            {
                100 => ("curl.exe", @"C:\Windows\System32\curl.exe", false),
                200 => ("mihomo.exe", @"D:\WorkSpace\Tools\Netch\release\bin\mihomo.exe", true),
                _ => ("other.exe", @"C:\Apps\other.exe", false)
            },
            localSet);

        // Outbound TCP send: local -> remote
        var (ep1, port1) = EtwNetworkMonitor.ResolveRemoteEndpoint(
            IPAddress.Parse("192.168.1.50"), 52000,
            IPAddress.Parse("1.1.1.1"), 443,
            isTcp: true, isSend: true, localSet);
        Assert.AreEqual("1.1.1.1", ep1.ToString());
        Assert.AreEqual(443, port1);

        // Inbound TCP recv with inverted addresses (PerfView #859 shape): daddr is local, saddr is remote
        var (ep2, port2) = EtwNetworkMonitor.ResolveRemoteEndpoint(
            IPAddress.Parse("::ffff:1.1.1.1"), 443,
            IPAddress.Parse("::ffff:192.168.1.50"), 52000,
            isTcp: true, isSend: false, localSet);
        Assert.AreEqual("1.1.1.1", ep2.ToString());
        Assert.AreEqual(443, port2);

        // Link-local IPv6 strips scope id
        var v6WithScope = IPAddress.Parse("fe80::1%12");
        Assert.AreEqual("fe80::1", EtwNetworkMonitor.NormalizeAddress(v6WithScope).ToString());

        // Noise vs real LAN / Fake-IP / WAN filtering
        Assert.IsTrue(EtwNetworkMonitor.ShouldIgnoreRemoteAddress(IPAddress.Loopback, 1080));
        Assert.IsTrue(EtwNetworkMonitor.ShouldIgnoreRemoteAddress(IPAddress.IPv6Loopback, 1080));
        Assert.IsTrue(EtwNetworkMonitor.ShouldIgnoreRemoteAddress(IPAddress.Parse("224.0.0.251"), 5353));
        Assert.IsTrue(EtwNetworkMonitor.ShouldIgnoreRemoteAddress(IPAddress.Broadcast, 67));
        Assert.IsTrue(EtwNetworkMonitor.ShouldIgnoreRemoteAddress(IPAddress.Parse("1.1.1.1"), 0));
        Assert.IsFalse(EtwNetworkMonitor.ShouldIgnoreRemoteAddress(IPAddress.Parse("192.168.1.1"), 80));
        Assert.IsFalse(EtwNetworkMonitor.ShouldIgnoreRemoteAddress(IPAddress.Parse("198.18.0.2"), 443));
        Assert.IsFalse(EtwNetworkMonitor.ShouldIgnoreRemoteAddress(IPAddress.Parse("1.1.1.1"), 443));

        var t0 = DateTimeOffset.FromUnixTimeMilliseconds(1_700_000_000_000);
        // Excluded PID is ignored
        monitor.RecordTrafficSample(200, 99, true, true, IPAddress.Parse("192.168.1.50"), 50000, IPAddress.Parse("8.8.8.8"), 443, 1200, t0, 1000);
        // Allowed TCP send + recv with same connId correlate into one flow
        monitor.RecordTrafficSample(100, 77, true, true, IPAddress.Parse("192.168.1.50"), 51000, IPAddress.Parse("1.1.1.1"), 443, 500, t0, 1000);
        monitor.RecordTrafficSample(100, 77, true, false, IPAddress.Parse("192.168.1.50"), 51000, IPAddress.Parse("1.1.1.1"), 443, 1500, t0.AddSeconds(1), 2000);
        // Allowed UDP send
        monitor.RecordTrafficSample(100, 0, false, true, IPAddress.Any, 53000, IPAddress.Parse("8.8.4.4"), 53, 80, t0, 1000);

        var snap1 = monitor.TakeSnapshot(t0.AddSeconds(2), 3000);
        Assert.HasCount(2, snap1.Active);
        Assert.IsEmpty(snap1.Closed);
        Assert.HasCount(2, snap1.NewEvents);

        var tcpFlow = snap1.Active.Single(c => c.Protocol == "TCP");
        Assert.IsTrue(tcpFlow.Id >= EtwNetworkMonitor.EtwIdBase);
        Assert.IsTrue(tcpFlow.IsDirect);
        Assert.IsFalse(tcpFlow.CanClose);
        Assert.AreEqual("curl.exe", tcpFlow.ProcessName);
        Assert.AreEqual(500UL, tcpFlow.UpBytes);
        Assert.AreEqual(1500UL, tcpFlow.DownBytes);

        // TCP disconnect moves TCP flow to closed; UDP sweeps after 45s idle
        monitor.RecordTcpDisconnect(100, 77, null, 0, null, 0, t0.AddSeconds(5));
        var snap2 = monitor.TakeSnapshot(t0.AddSeconds(50), 51_000);
        Assert.IsEmpty(snap2.Active);
        Assert.HasCount(2, snap2.Closed);
        Assert.AreEqual("TCP disconnected", snap2.Closed.Single(c => c.Protocol == "TCP").CloseReason);
        Assert.AreEqual("UDP idle timeout", snap2.Closed.Single(c => c.Protocol == "UDP").CloseReason);
    }

    [TestMethod]
    public void ConnectionTrackerMergesEtwDirectFlowsDedupsProxiedAndFiltersRoutes()
    {
        var tracker = new ConnectionTrackerService();
        var now = DateTimeOffset.FromUnixTimeMilliseconds(1_700_000_010_000);

        var nativeDump = JsonSerializer.Deserialize<ProcessConnectionsDump>("""
            {
              "active": [
                { "id": 10, "protocol": "TCP", "state": "Active", "action": "PROXY", "pid": 4242, "processName": "game.exe", "remoteIp": "1.1.1.1", "remotePort": 443, "upBytes": 1000, "downBytes": 2000, "matchedRule": "game\\.exe$", "ruleSource": "Process.Handle" }
              ],
              "closed": []
            }
            """)!;
        tracker.ApplyConnectionSnapshot(nativeDump, 1.0);

        var etwActive = new List<ProcessConnectionSnapshot>
        {
            // Duplicate of native proxied 4-tuple: must be suppressed
            new()
            {
                Id = EtwNetworkMonitor.EtwIdBase + 1,
                Protocol = "TCP",
                State = "Active",
                Action = "DIRECT",
                Pid = 4242,
                ProcessName = "game.exe",
                RemoteIp = "1.1.1.1",
                RemotePort = 443,
                UpBytes = 1000,
                DownBytes = 2000,
                MatchedRule = "DIRECT",
                RuleSource = "ETW.Direct"
            },
            // Separate unmatched direct connection: must be retained with speeds
            new()
            {
                Id = EtwNetworkMonitor.EtwIdBase + 2,
                Protocol = "UDP",
                State = "Active",
                Action = "DIRECT",
                Pid = 9001,
                ProcessName = "curl.exe",
                RemoteIp = "8.8.8.8",
                RemotePort = 443,
                UpBytes = 500,
                DownBytes = 4000,
                MatchedRule = "DIRECT",
                RuleSource = "ETW.Direct"
            }
        };

        tracker.ApplyEtwSnapshot(etwActive, Array.Empty<ProcessConnectionSnapshot>(), 1.0, now);

        Assert.HasCount(2, tracker.ActiveConnections);
        Assert.AreEqual(500UL, tracker.DirectUpSpeedBps);
        Assert.AreEqual(4000UL, tracker.DirectDownSpeedBps);

        var proxiedOnly = ConnectionTrackerService.FilterConnections(tracker.ActiveConnections, null, "ALL", "PROXY");
        Assert.HasCount(1, proxiedOnly);
        Assert.IsFalse(proxiedOnly[0].IsDirect);
        Assert.IsTrue(proxiedOnly[0].CanClose);

        var directOnly = ConnectionTrackerService.FilterConnections(tracker.ActiveConnections, null, "ALL", "DIRECT");
        Assert.HasCount(1, directOnly);
        Assert.IsTrue(directOnly[0].IsDirect);
        Assert.IsFalse(directOnly[0].CanClose);
        Assert.IsFalse(tracker.CloseConnection(directOnly[0].Id));

        // Event dedup & BYPASS filter
        tracker.IngestEvents(new ProcessConnectionEventBatch
        {
            NextSeq = 1,
            Events =
            [
                new ProcessConnectionEvent { Seq = 1, TimestampMs = 1_700_000_009_000, ConnectionId = 10, Action = "PROXY", Protocol = "TCP", Pid = 4242, ProcessName = "game.exe", RemoteIp = "1.1.1.1", RemotePort = 443 }
            ]
        }, writeToFileLog: false);

        var addedDirect = tracker.IngestDirectEvents(
            [
                // Matches active/recent native tuple -> dropped
                new ProcessConnectionEvent { TimestampMs = 1_700_000_010_000, ConnectionId = EtwNetworkMonitor.EtwIdBase + 1, Action = "DIRECT", Protocol = "TCP", Pid = 4242, ProcessName = "game.exe", RemoteIp = "1.1.1.1", RemotePort = 443 },
                // Distinct direct flow -> kept with seq 2
                new ProcessConnectionEvent { TimestampMs = 1_700_000_010_000, ConnectionId = EtwNetworkMonitor.EtwIdBase + 2, Action = "DIRECT", Protocol = "UDP", Pid = 9001, ProcessName = "curl.exe", RemoteIp = "8.8.8.8", RemotePort = 443, MatchedRule = "DIRECT", RuleSource = "ETW.Direct" }
            ],
            writeToFileLog: false,
            nowUtcOverride: now);

        Assert.HasCount(1, addedDirect);
        Assert.AreEqual(2UL, addedDirect[0].Seq);

        var bypassEvents = ConnectionTrackerService.FilterEvents(tracker.Events, null, false, "BYPASS", false, out var err);
        Assert.IsNull(err);
        Assert.HasCount(1, bypassEvents);
        Assert.AreEqual("DIRECT", bypassEvents[0].Event.Action);
    }

    [TestMethod]
    public void SystemProcessProtectionAndDirectedBroadcastFilterWork()
    {
        Assert.AreEqual("192.168.123.255", EtwNetworkMonitor.TryComputeDirectedBroadcast(
            IPAddress.Parse("192.168.123.50"), IPAddress.Parse("255.255.255.0"))?.ToString());
        Assert.AreEqual("10.1.255.255", EtwNetworkMonitor.TryComputeDirectedBroadcast(
            IPAddress.Parse("10.1.2.3"), IPAddress.Parse("255.255.0.0"))?.ToString());
        Assert.IsNull(EtwNetworkMonitor.TryComputeDirectedBroadcast(
            IPAddress.Parse("192.168.123.50"), IPAddress.Parse("255.255.255.255")));
        Assert.IsNull(EtwNetworkMonitor.TryComputeDirectedBroadcast(
            IPAddress.Any, IPAddress.Any));

        var localSet = new HashSet<IPAddress> { IPAddress.Parse("192.168.123.50") };
        var broadcastSet = new HashSet<IPAddress> { IPAddress.Parse("192.168.123.255") };
        Assert.IsTrue(EtwNetworkMonitor.ShouldIgnoreRemoteAddress(IPAddress.Parse("192.168.123.255"), 138, broadcastSet));
        Assert.IsFalse(EtwNetworkMonitor.ShouldIgnoreRemoteAddress(IPAddress.Parse("192.168.123.10"), 445, broadcastSet));

        var monitor = new EtwNetworkMonitor(
            pid => ("System", "System", false),
            localSet,
            broadcastSet);

        var t0 = DateTimeOffset.FromUnixTimeMilliseconds(1_700_000_020_000);
        // System PID 4 and Idle PID 0 are always filtered
        monitor.RecordTrafficSample(4, 0, false, false, IPAddress.Parse("192.168.123.210"), 138, IPAddress.Parse("192.168.123.50"), 138, 183, t0, 1000);
        monitor.RecordTrafficSample(0, 0, true, false, IPAddress.Parse("192.168.123.50"), 445, IPAddress.Parse("192.168.123.10"), 49000, 100, t0, 1000);
        // Directed subnet broadcast from any process is filtered
        monitor.RecordTrafficSample(100, 0, false, true, IPAddress.Parse("192.168.123.50"), 138, IPAddress.Parse("192.168.123.255"), 138, 210, t0, 1000);

        var snap = monitor.TakeSnapshot(t0.AddSeconds(1), 2000);
        Assert.IsEmpty(snap.Active);
        Assert.IsEmpty(snap.NewEvents);

        Assert.AreEqual(ProcessRuleSafety.Blocked, ConnectionTrackerService.ClassifyProcessRuleSafety("System"));
        Assert.AreEqual(ProcessRuleSafety.Blocked, ConnectionTrackerService.ClassifyProcessRuleSafety("System$"));
        Assert.AreEqual(ProcessRuleSafety.Blocked, ConnectionTrackerService.ClassifyProcessRuleSafety("Idle"));
        Assert.AreEqual(ProcessRuleSafety.Blocked, ConnectionTrackerService.ClassifyProcessRuleSafety("Unknown"));
        Assert.AreEqual(ProcessRuleSafety.Blocked, ConnectionTrackerService.ClassifyProcessRuleSafety("lsass.exe"));
        Assert.AreEqual(ProcessRuleSafety.Blocked, ConnectionTrackerService.ClassifyProcessRuleSafety(@"C:\Windows\System32\csrss.exe"));
        Assert.AreEqual(ProcessRuleSafety.Blocked, ConnectionTrackerService.ClassifyProcessRuleSafety(@"^lsass\.exe$"));
        Assert.AreEqual(ProcessRuleSafety.Blocked, ConnectionTrackerService.ClassifyProcessRuleSafety("services.exe"));
        Assert.AreEqual(ProcessRuleSafety.Caution, ConnectionTrackerService.ClassifyProcessRuleSafety("svchost.exe"));
        Assert.AreEqual(ProcessRuleSafety.Caution, ConnectionTrackerService.ClassifyProcessRuleSafety(@"svchost\.exe$"));
        Assert.AreEqual(ProcessRuleSafety.Normal, ConnectionTrackerService.ClassifyProcessRuleSafety("chrome.exe"));
        Assert.AreEqual(ProcessRuleSafety.Normal, ConnectionTrackerService.ClassifyProcessRuleSafety("NatTypeTester.exe"));

        var tracker = new ConnectionTrackerService();
        Assert.IsFalse(tracker.TryApplyHotRule(HotRuleKind.HandleProcess, "System$", "System", out var sysErr));
        Assert.IsFalse(string.IsNullOrWhiteSpace(sysErr));
        Assert.IsFalse(tracker.TryApplyHotRule(HotRuleKind.HandleProcess, @"lsass\.exe$", "lsass.exe", out var lsassErr));
        Assert.IsFalse(string.IsNullOrWhiteSpace(lsassErr));
        Assert.IsFalse(tracker.HasUnsavedHotRules);
    }

    [TestMethod]
    public void EtwMultiSocketTcpFlowStaysActiveUntilAllSocketsClose()
    {
        var localSet = new HashSet<IPAddress> { IPAddress.Parse("192.168.1.10") };
        var monitor = new EtwNetworkMonitor(
            pid => ($"browser-{pid}.exe", $@"C:\Apps\browser-{pid}.exe", false),
            localSet);

        var t0 = DateTimeOffset.FromUnixTimeMilliseconds(1_700_000_030_000);
        var localIp = IPAddress.Parse("192.168.1.10");
        var remoteIp = IPAddress.Parse("93.184.216.34");

        // Two concurrent TCP sockets from the same process to the same remote endpoint
        monitor.RecordTrafficSample(5100, 101, true, true, localIp, 50101, remoteIp, 443, 400, t0, 1000);
        monitor.RecordTrafficSample(5100, 102, true, false, remoteIp, 443, localIp, 50102, 1200, t0.AddMilliseconds(50), 1050);

        var snap1 = monitor.TakeSnapshot(t0.AddMilliseconds(100), 1100);
        Assert.HasCount(1, snap1.Active);
        Assert.IsEmpty(snap1.Closed);
        Assert.AreEqual(400UL, snap1.Active[0].UpBytes);
        Assert.AreEqual(1200UL, snap1.Active[0].DownBytes);

        // Disconnecting socket 101 must keep the aggregated flow active while 102 remains open
        monitor.RecordTcpDisconnect(5100, 101, localIp, 50101, remoteIp, 443, t0.AddMilliseconds(200));
        var snap2 = monitor.TakeSnapshot(t0.AddMilliseconds(250), 1250);
        Assert.HasCount(1, snap2.Active);
        Assert.IsEmpty(snap2.Closed);

        // Disconnecting socket 102 closes the aggregated flow
        monitor.RecordTcpDisconnect(5100, 102, localIp, 50102, remoteIp, 443, t0.AddMilliseconds(300));
        var snap3 = monitor.TakeSnapshot(t0.AddMilliseconds(350), 1350);
        Assert.IsEmpty(snap3.Active);
        Assert.HasCount(1, snap3.Closed);
        Assert.AreEqual(400UL, snap3.Closed[0].UpBytes);
        Assert.AreEqual(1200UL, snap3.Closed[0].DownBytes);
    }
}
