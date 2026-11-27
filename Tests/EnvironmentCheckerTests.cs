#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Netch;
using Netch.Enums;
using Netch.Models;
using Netch.Models.Modes;
using Netch.Models.Modes.ProcessMode;
using Netch.Models.Modes.ShareMode;
using Netch.Models.Modes.TunMode;
using Netch.Models.Modes.WebProxyMode;
using Netch.Servers;
using Netch.Services;
using Netch.Utils;

namespace Tests;

[TestClass]
[DoNotParallelize]
public class EnvironmentCheckerTests
{
    private sealed class FakeEnvironmentProbe : IEnvironmentProbe
    {
        public int WindowsBuild { get; set; } = 26100;
        public bool Arm64Emulated { get; set; }
        public string SystemDirectory { get; set; } = @"C:\Windows\System32";
        public bool Avx2V3 { get; set; } = true;
        public bool? BfeRunning { get; set; } = true;
        public HashSet<string> ExistingFiles { get; } = new(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> LoadableLibraries { get; } = new(StringComparer.OrdinalIgnoreCase);
        public int FileExistsCallCount { get; private set; }
        public int CanLoadNativeLibraryCallCount { get; private set; }
        public int IsServiceRunningCallCount { get; private set; }
        public HashSet<string> LoadedLibraryQueries { get; } = new(StringComparer.OrdinalIgnoreCase);

        public void SeedAllBinaries(string baseDir)
        {
            foreach (var rel in new[]
                     {
                         @"bin\mihomo.exe",
                         @"bin\xray.exe",
                         @"bin\sing_box\sing-box.exe",
                         @"bin\sing_box\libcronet.dll",
                         @"bin\aiodns.conf",
                         @"bin\nfdriver.sys",
                         @"bin\nfapi.dll",
                         @"bin\Redirector.bin",
                         @"bin\wintun.dll",
                         @"bin\tun2socks-windows-amd64-v3.exe",
                         @"bin\RouteHelper.bin",
                         @"bin\pcap2socks.exe"
                     })
            {
                ExistingFiles.Add(Path.Combine(baseDir, rel));
            }
        }

        public bool IsWindowsVersionAtLeast(int major, int minor, int build) =>
            major < 10 || (major == 10 && minor == 0 && WindowsBuild >= build);

        public bool IsArm64Emulated() => Arm64Emulated;

        public bool FileExists(string path)
        {
            FileExistsCallCount++;
            return ExistingFiles.Contains(path);
        }

        public bool CanLoadNativeLibrary(string libraryName)
        {
            CanLoadNativeLibraryCallCount++;
            LoadedLibraryQueries.Add(libraryName);
            return LoadableLibraries.Contains(libraryName);
        }

        public bool SupportsX8664V3() => Avx2V3;

        public bool? IsServiceRunning(string serviceName)
        {
            IsServiceRunningCallCount++;
            return string.Equals(serviceName, "BFE", StringComparison.OrdinalIgnoreCase) ? BfeRunning : null;
        }

        public void ResetCallCounts()
        {
            FileExistsCallCount = 0;
            CanLoadNativeLibraryCallCount = 0;
            IsServiceRunningCallCount = 0;
            LoadedLibraryQueries.Clear();
        }
    }

    private Setting _savedSettings = null!;

    [TestInitialize]
    public void Setup()
    {
        i18N.Load("en-US");
        _savedSettings = Global.Settings;
        Global.Settings = new Setting();
    }

    [TestCleanup]
    public void Cleanup()
    {
        Global.Settings = _savedSettings;
        i18N.Load("en-US");
    }

    private static FakeEnvironmentProbe CreateHealthyProbe(string baseDir)
    {
        var probe = new FakeEnvironmentProbe();
        probe.SeedAllBinaries(baseDir);
        probe.LoadableLibraries.Add("packet.dll");
        probe.LoadableLibraries.Add("vcruntime140.dll");
        return probe;
    }

    [TestMethod]
    public void InspectChecksWindows1809BoundaryAndArm64Emulation()
    {
        const string baseDir = @"D:\Netch";
        var probe = CreateHealthyProbe(baseDir);

        probe.WindowsBuild = 17762;
        Assert.IsFalse(EnvironmentChecker.Inspect(baseDir, probe).IsOsSupported);

        probe.WindowsBuild = 17763;
        Assert.IsTrue(EnvironmentChecker.Inspect(baseDir, probe).IsOsSupported);

        probe.Arm64Emulated = true;
        var report = EnvironmentChecker.Inspect(baseDir, probe);
        Assert.IsTrue(report.IsArm64Emulated);

        var driverIssues = EnvironmentChecker.GetIssuesForModes([new Redirector(), new TunMode()], baseDir, probe);
        Assert.AreEqual(1, driverIssues.Count(i => i.Code == "arm64_driver_unsupported"));

        var webIssues = EnvironmentChecker.GetIssuesForModes([new WebProxyMode()], baseDir, probe);
        Assert.IsEmpty(webIssues);
    }

    [TestMethod]
    public void InspectDistinguishesNpcapStatesAndChecksVcRedistOnlyForShareMode()
    {
        const string baseDir = @"D:\Netch";
        var probe = CreateHealthyProbe(baseDir);

        Assert.IsEmpty(EnvironmentChecker.GetIssuesForModes([new ShareMode()], baseDir, probe));

        probe.LoadableLibraries.Remove("packet.dll");
        var missingNpcap = EnvironmentChecker.Inspect(baseDir, probe);
        Assert.AreEqual(NpcapStatus.Missing, missingNpcap.Npcap);
        var missingIssues = EnvironmentChecker.GetIssuesForModes([new ShareMode()], baseDir, probe);
        CollectionAssert.Contains(missingIssues.Select(i => i.Code).ToArray(), "npcap_missing");

        probe.ExistingFiles.Add(Path.Combine(probe.SystemDirectory, "Npcap", "Packet.dll"));
        var noCompat = EnvironmentChecker.Inspect(baseDir, probe);
        Assert.AreEqual(NpcapStatus.MissingWinPcapCompat, noCompat.Npcap);
        var noCompatIssues = EnvironmentChecker.GetIssuesForModes([new ShareMode()], baseDir, probe);
        CollectionAssert.Contains(noCompatIssues.Select(i => i.Code).ToArray(), "npcap_winpcap_compat");

        probe.LoadableLibraries.Add("packet.dll");
        probe.LoadableLibraries.Remove("vcruntime140.dll");
        var vcMissingShare = EnvironmentChecker.GetIssuesForModes([new ShareMode()], baseDir, probe);
        Assert.AreEqual(1, vcMissingShare.Count);
        Assert.AreEqual("vcredist_x64_missing", vcMissingShare[0].Code);

        Assert.IsEmpty(EnvironmentChecker.GetIssuesForModes([new Redirector(), new WebProxyMode()], baseDir, probe));
        Assert.IsEmpty(EnvironmentChecker.GetIssuesForModes([new TunMode()], baseDir, probe));
    }

    [TestMethod]
    public void InspectChecksAvx2ForTunModeAndBfeForProcessMode()
    {
        const string baseDir = @"D:\Netch";
        var probe = CreateHealthyProbe(baseDir);

        probe.Avx2V3 = false;
        probe.BfeRunning = false;

        var tunIssues = EnvironmentChecker.GetIssuesForModes([new TunMode()], baseDir, probe);
        Assert.AreEqual(1, tunIssues.Count);
        Assert.AreEqual("cpu_missing_avx2", tunIssues[0].Code);

        var processIssues = EnvironmentChecker.GetIssuesForModes([new Redirector()], baseDir, probe);
        Assert.AreEqual(1, processIssues.Count);
        Assert.AreEqual("bfe_service_stopped", processIssues[0].Code);

        probe.BfeRunning = null;
        Assert.IsEmpty(EnvironmentChecker.GetIssuesForModes([new Redirector()], baseDir, probe));
        Assert.IsEmpty(EnvironmentChecker.GetIssuesForModes([new ShareMode(), new WebProxyMode()], baseDir, probe));
    }

    [TestMethod]
    public void InspectDetectsMissingBinFilesAndRespectsRootSingboxLayout()
    {
        const string baseDir = @"D:\Netch";
        var probe = CreateHealthyProbe(baseDir);

        // Root bin\sing-box.exe + bin\libcronet.dll layout is also supported by SingboxController.
        probe.ExistingFiles.Remove(Path.Combine(baseDir, @"bin\sing_box\sing-box.exe"));
        probe.ExistingFiles.Remove(Path.Combine(baseDir, @"bin\sing_box\libcronet.dll"));
        probe.ExistingFiles.Add(Path.Combine(baseDir, @"bin\sing-box.exe"));
        probe.ExistingFiles.Add(Path.Combine(baseDir, @"bin\libcronet.dll"));

        var report = EnvironmentChecker.Inspect(baseDir, probe);
        Assert.IsEmpty(report.AllMissingBinFiles);

        probe.ExistingFiles.Remove(Path.Combine(baseDir, @"bin\libcronet.dll"));
        var naive = new NaiveServer { Address = "1.2.3.4", Port = 443, Username = "u", Password = "p" };
        var coreIssues = EnvironmentChecker.GetIssuesForBuiltInCore(naive, [new Redirector()], baseDir, probe);
        Assert.AreEqual(1, coreIssues.Count);
        Assert.AreEqual("missing_core_files", coreIssues[0].Code);
        Assert.Contains("bin\\libcronet.dll", coreIssues[0].Detail);

        // Direct SOCKS5 without auth/HTTP requirement needs no built-in core binary.
        probe.ExistingFiles.Remove(Path.Combine(baseDir, @"bin\mihomo.exe"));
        var socks = new SocksServer("127.0.0.1", 1080);
        Assert.IsEmpty(EnvironmentChecker.GetIssuesForBuiltInCore(socks, [new Redirector()], baseDir, probe));

        // ExternalCli in WebProxy mode requires mihomo HTTP bridge, whereas Process mode does not.
        var ext = new ExternalCliServer { ProtoExtra = new ProtocolExtraItem { CliExecutable = @"C:\Tools\cli.exe" } };
        Assert.IsEmpty(EnvironmentChecker.GetIssuesForBuiltInCore(ext, [new Redirector()], baseDir, probe));
        var extWebIssues = EnvironmentChecker.GetIssuesForBuiltInCore(ext, [new WebProxyMode()], baseDir, probe);
        Assert.AreEqual(1, extWebIssues.Count);
        Assert.Contains("bin\\mihomo.exe", extWebIssues[0].Detail);
    }

    [TestMethod]
    public void EnsureReadyThrowsDependencyExceptionWithStructuredIssuesAndDiagnostics()
    {
        const string baseDir = @"D:\Netch";
        var probe = CreateHealthyProbe(baseDir);
        probe.LoadableLibraries.Remove("packet.dll");
        probe.LoadableLibraries.Remove("vcruntime140.dll");
        probe.BfeRunning = true;

        var server = new ShadowsocksServer { Address = "1.2.3.4", Port = 8388, Password = "pw" };
        server.ProtoExtra.SsMethod = "aes-256-gcm";
        Mode[] modes = [new Redirector(), new ShareMode()];

        // Healthy Process mode + failing Share mode should scope the title strictly to Share mode.
        var shareOnlyEx = Assert.ThrowsExactly<DependencyException>(() => EnvironmentChecker.EnsureReady(server, modes, baseDir, probe));
        Assert.AreEqual("Share mode", shareOnlyEx.ScopeTitle);
        Assert.AreEqual(2, shareOnlyEx.Issues.Count);
        CollectionAssert.AreEquivalent(
            new[] { "npcap_missing", "vcredist_x64_missing" },
            shareOnlyEx.Issues.Select(i => i.Code).ToArray());

        probe.BfeRunning = false;
        var ex = Assert.ThrowsExactly<DependencyException>(() => EnvironmentChecker.EnsureReady(server, modes, baseDir, probe));
        Assert.IsInstanceOfType<MessageException>(ex);
        Assert.AreEqual("Process mode + Share mode", ex.ScopeTitle);
        Assert.AreEqual(3, ex.Issues.Count);
        CollectionAssert.AreEquivalent(
            new[] { "bfe_service_stopped", "npcap_missing", "vcredist_x64_missing" },
            ex.Issues.Select(i => i.Code).ToArray());
        Assert.Contains("Npcap", ex.Message);

        var diag = EnvironmentChecker.FormatDiagnostics(EnvironmentChecker.Inspect(baseDir, probe));
        Assert.Contains("Core files: Complete", diag);
        Assert.Contains("BFE service: Stopped", diag);
        Assert.Contains("Npcap: Not installed", diag);
        Assert.Contains("VC++ 2015-2022 (x64): Not installed", diag);
    }

    [TestMethod]
    public void TargetedModeProbesArm64TunScopeAndCliCustomCoreHttpBridge()
    {
        const string baseDir = @"D:\Netch";
        var probe = CreateHealthyProbe(baseDir);

        probe.ResetCallCounts();
        Assert.IsEmpty(EnvironmentChecker.GetIssuesForModes([new WebProxyMode()], baseDir, probe));
        Assert.AreEqual(0, probe.FileExistsCallCount);
        Assert.AreEqual(0, probe.CanLoadNativeLibraryCallCount);
        Assert.AreEqual(0, probe.IsServiceRunningCallCount);

        probe.ResetCallCounts();
        Assert.IsEmpty(EnvironmentChecker.GetIssuesForModes([new Redirector()], baseDir, probe));
        Assert.AreEqual(0, probe.CanLoadNativeLibraryCallCount);
        Assert.IsEmpty(probe.LoadedLibraryQueries);

        probe.Arm64Emulated = true;
        var arm64Server = new ShadowsocksServer { Address = "1.2.3.4", Port = 8388, Password = "pw" };
        arm64Server.ProtoExtra.SsMethod = "aes-256-gcm";
        var arm64Ex = Assert.ThrowsExactly<DependencyException>(() =>
            EnvironmentChecker.EnsureReady(arm64Server, [new TunMode()], baseDir, probe));
        Assert.AreEqual("TUN mode", arm64Ex.ScopeTitle);
        Assert.IsTrue(arm64Ex.Issues.All(i => i.Scope == DependencyScope.TunMode));

        var customCore = new CustomCore
        {
            Id = "custom-ss-cli",
            Name = "Custom SS CLI",
            Protocol = EConfigType.Shadowsocks,
            ExecutablePath = @"C:\Tools\sslocal.exe",
            Mode = CustomCoreMode.CliArguments,
            ArgumentsTemplate = "-s {address} -p {port} -b {local_addr} -l {local_port}"
        };
        Global.Settings.CustomCores = [customCore];

        var customServer = new ShadowsocksServer { Address = "1.2.3.4", Port = 8388, Password = "pw" };
        customServer.ProtoExtra.SsMethod = "aes-256-gcm";
        customServer.ProtoExtra.CustomCoreId = customCore.Id;

        probe.ExistingFiles.Remove(Path.Combine(baseDir, @"bin\mihomo.exe"));
        Assert.IsEmpty(EnvironmentChecker.GetIssuesForBuiltInCore(customServer, [new Redirector()], baseDir, probe));
        var webBridgeIssues = EnvironmentChecker.GetIssuesForBuiltInCore(customServer, [new WebProxyMode()], baseDir, probe);
        Assert.AreEqual(1, webBridgeIssues.Count);
        Assert.Contains("bin\\mihomo.exe", webBridgeIssues[0].Detail);
    }
}
