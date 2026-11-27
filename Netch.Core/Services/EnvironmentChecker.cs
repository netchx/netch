using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.ServiceProcess;
using System.Text;
using Netch.Enums;
using Netch.Models;
using Netch.Models.Modes;
using Netch.Servers;
using Netch.Utils;
using Windows.Win32;
using Windows.Win32.System.SystemInformation;

namespace Netch.Services;

internal interface IEnvironmentProbe
{
    bool IsWindowsVersionAtLeast(int major, int minor, int build);

    bool IsArm64Emulated();

    bool FileExists(string path);

    bool CanLoadNativeLibrary(string libraryName);

    string SystemDirectory { get; }

    bool SupportsX8664V3();

    bool? IsServiceRunning(string serviceName);
}

internal sealed class DefaultEnvironmentProbe : IEnvironmentProbe
{
    public static DefaultEnvironmentProbe Instance { get; } = new();

    private DefaultEnvironmentProbe()
    {
    }

    public bool IsWindowsVersionAtLeast(int major, int minor, int build) =>
        OperatingSystem.IsWindowsVersionAtLeast(major, minor, build);

    public bool IsArm64Emulated()
    {
        try
        {
            using var currentProcess = Process.GetCurrentProcess();
            if (!PInvoke.IsWow64Process2(currentProcess.SafeHandle, out _, out var nativeMachine))
                return false;

            return nativeMachine == IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_ARM64;
        }
        catch (EntryPointNotFoundException)
        {
            return false;
        }
    }

    public bool FileExists(string path) => File.Exists(path);

    public bool CanLoadNativeLibrary(string libraryName)
    {
        if (!NativeLibrary.TryLoad(libraryName, out var handle))
            return false;

        NativeLibrary.Free(handle);
        return true;
    }

    public string SystemDirectory => Environment.SystemDirectory;

    public bool SupportsX8664V3() =>
        Avx2.IsSupported
        && Bmi1.IsSupported
        && Bmi2.IsSupported
        && Fma.IsSupported
        && Lzcnt.IsSupported
        && Popcnt.IsSupported;

    public bool? IsServiceRunning(string serviceName)
    {
        try
        {
            using var service = new ServiceController(serviceName);
            return service.Status == ServiceControllerStatus.Running;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        catch (Win32Exception)
        {
            return null;
        }
    }
}

public sealed record EnvironmentReport(
    bool IsOsSupported,
    bool IsArm64Emulated,
    IReadOnlyList<string> MissingCoreFiles,
    IReadOnlyList<string> MissingProcessFiles,
    IReadOnlyList<string> MissingTunFiles,
    IReadOnlyList<string> MissingShareFiles,
    NpcapStatus Npcap,
    bool HasVcRedistX64,
    bool SupportsAvx2V3,
    bool? IsBfeRunning)
{
    public IReadOnlyList<string> AllMissingBinFiles { get; } = MissingCoreFiles
        .Concat(MissingProcessFiles)
        .Concat(MissingTunFiles)
        .Concat(MissingShareFiles)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();
}

public static class EnvironmentChecker
{
    public const int MinimumWindowsBuild = 17763;
    public const string NpcapDownloadUrl = "https://npcap.com/";
    public const string VcRedistX64DownloadUrl = "https://aka.ms/vs/17/release/vc_redist.x64.exe";
    public const string DotNet10DesktopDownloadUrl = "https://dotnet.microsoft.com/download/dotnet/10.0";

    private const string MihomoFile = "bin\\mihomo.exe";
    private const string XrayFile = "bin\\xray.exe";
    private const string RootSingboxFile = "bin\\sing-box.exe";
    private const string RootLibcronetFile = "bin\\libcronet.dll";
    private const string SubdirSingboxFile = "bin\\sing_box\\sing-box.exe";
    private const string SubdirLibcronetFile = "bin\\sing_box\\libcronet.dll";
    private const string AioDnsRuleFile = Constants.AioDnsRuleFile;

    private static readonly string[] ProcessFiles =
    [
        Constants.NFDriver,
        "bin\\nfapi.dll",
        "bin\\Redirector.bin"
    ];

    private static readonly string[] TunFiles =
    [
        Constants.WintunDllFile,
        Constants.TUN2SocksFile,
        "bin\\RouteHelper.bin"
    ];

    private static readonly string[] ShareFiles =
    [
        "bin\\pcap2socks.exe"
    ];

    public static bool IsOsSupported => DefaultEnvironmentProbe.Instance.IsWindowsVersionAtLeast(10, 0, MinimumWindowsBuild);

    public static EnvironmentReport Inspect(string? baseDir = null) =>
        Inspect(NormalizeBaseDir(baseDir), DefaultEnvironmentProbe.Instance);

    internal static EnvironmentReport Inspect(string baseDir, IEnvironmentProbe probe)
    {
        ArgumentNullException.ThrowIfNull(baseDir);
        ArgumentNullException.ThrowIfNull(probe);

        var missingCore = new List<string>();
        foreach (var file in new[] { MihomoFile, XrayFile, AioDnsRuleFile })
        {
            if (!probe.FileExists(Path.Combine(baseDir, file)))
                missingCore.Add(file);
        }

        var hasRootSingbox = probe.FileExists(Path.Combine(baseDir, RootSingboxFile));
        var hasSubdirSingbox = probe.FileExists(Path.Combine(baseDir, SubdirSingboxFile));
        if (!hasRootSingbox && !hasSubdirSingbox)
        {
            missingCore.Add(SubdirSingboxFile);
            if (!probe.FileExists(Path.Combine(baseDir, SubdirLibcronetFile)))
                missingCore.Add(SubdirLibcronetFile);
        }
        else
        {
            var cronetFile = hasRootSingbox ? RootLibcronetFile : SubdirLibcronetFile;
            if (!probe.FileExists(Path.Combine(baseDir, cronetFile)))
                missingCore.Add(cronetFile);
        }

        var missingProcess = CollectMissingFiles(baseDir, ProcessFiles, probe);
        var missingTun = CollectMissingFiles(baseDir, TunFiles, probe);
        var missingShare = CollectMissingFiles(baseDir, ShareFiles, probe);

        var npcap = DetectNpcap(probe);
        var hasVcRedist = probe.CanLoadNativeLibrary("vcruntime140.dll");

        return new EnvironmentReport(
            probe.IsWindowsVersionAtLeast(10, 0, MinimumWindowsBuild),
            probe.IsArm64Emulated(),
            missingCore,
            missingProcess,
            missingTun,
            missingShare,
            npcap,
            hasVcRedist,
            probe.SupportsX8664V3(),
            probe.IsServiceRunning("BFE"));
    }

    public static IReadOnlyList<DependencyIssue> GetIssuesForModes(IReadOnlyList<Mode> modes, string? baseDir = null) =>
        GetIssuesForModes(modes, NormalizeBaseDir(baseDir), DefaultEnvironmentProbe.Instance);

    internal static IReadOnlyList<DependencyIssue> GetIssuesForModes(
        IReadOnlyList<Mode> modes,
        string baseDir,
        IEnvironmentProbe probe)
    {
        ArgumentNullException.ThrowIfNull(modes);
        ArgumentNullException.ThrowIfNull(baseDir);
        ArgumentNullException.ThrowIfNull(probe);

        if (modes.Count == 0)
            return Array.Empty<DependencyIssue>();

        return CollectModeIssues(modes, baseDir, probe);
    }

    public static IReadOnlyList<DependencyIssue> GetIssuesForBuiltInCore(
        Server server,
        IReadOnlyList<Mode> modes,
        string? baseDir = null) =>
        GetIssuesForBuiltInCore(server, modes, NormalizeBaseDir(baseDir), DefaultEnvironmentProbe.Instance);

    internal static IReadOnlyList<DependencyIssue> GetIssuesForBuiltInCore(
        Server server,
        IReadOnlyList<Mode> modes,
        string baseDir,
        IEnvironmentProbe probe)
    {
        ArgumentNullException.ThrowIfNull(server);
        ArgumentNullException.ThrowIfNull(modes);
        ArgumentNullException.ThrowIfNull(baseDir);
        ArgumentNullException.ThrowIfNull(probe);

        var requireLocalHttp = modes.Any(m =>
            ModeService.GetModeFeatures(m.Type).HasFlag(ModeFeature.RequireLocalHttpProxy));
        var allSupportSocks5Auth = modes.Count == 0 || modes.All(m =>
            ModeService.GetModeFeatures(m.Type).HasFlag(ModeFeature.SupportSocks5Auth));
        var customCore = server is ExternalCliServer ? null : CustomCoreHelper.ResolveForServer(server);

        if (!requireLocalHttp &&
            server is SocksServer socks5 &&
            socks5.ProtoExtra.Uot != true &&
            customCore == null &&
            (!socks5.Auth() || allSupportSocks5Auth))
        {
            return Array.Empty<DependencyIssue>();
        }

        var requiredFiles = new List<string>();
        if (server is ExternalCliServer || customCore?.Mode == CustomCoreMode.CliArguments)
        {
            if (requireLocalHttp && !probe.FileExists(Path.Combine(baseDir, MihomoFile)))
                requiredFiles.Add(MihomoFile);
        }
        else if (customCore == null)
        {
            switch (CoreSelector.SelectCore(server))
            {
                case CoreSelector.CoreKind.Xray:
                    if (!probe.FileExists(Path.Combine(baseDir, XrayFile)))
                        requiredFiles.Add(XrayFile);
                    break;
                case CoreSelector.CoreKind.Singbox:
                    var hasRootSingbox = probe.FileExists(Path.Combine(baseDir, RootSingboxFile));
                    var hasSubdirSingbox = probe.FileExists(Path.Combine(baseDir, SubdirSingboxFile));
                    if (!hasRootSingbox && !hasSubdirSingbox)
                        requiredFiles.Add(SubdirSingboxFile);

                    if (server.ConfigType == EConfigType.Naive)
                    {
                        var cronetFile = hasRootSingbox ? RootLibcronetFile : SubdirLibcronetFile;
                        if (!probe.FileExists(Path.Combine(baseDir, cronetFile)))
                            requiredFiles.Add(cronetFile);
                    }
                    break;
                default:
                    if (!probe.FileExists(Path.Combine(baseDir, MihomoFile)))
                        requiredFiles.Add(MihomoFile);
                    break;
            }
        }

        if (requiredFiles.Count == 0)
            return Array.Empty<DependencyIssue>();

        return
        [
            new DependencyIssue(
                "missing_core_files",
                DependencyScope.CoreFiles,
                i18N.Translate("Missing core binary files"),
                string.Format(
                    i18N.Translate("Missing: {0}. Extract the full release archive and check antivirus quarantine."),
                    string.Join(", ", requiredFiles)),
                DependencyActionKind.OpenDirectory,
                i18N.Translate("Open directory"),
                baseDir)
        ];
    }

    public static void EnsureReady(Server server, IReadOnlyList<Mode> modes, string? baseDir = null) =>
        EnsureReady(server, modes, NormalizeBaseDir(baseDir), DefaultEnvironmentProbe.Instance);

    internal static void EnsureReady(
        Server server,
        IReadOnlyList<Mode> modes,
        string baseDir,
        IEnvironmentProbe probe)
    {
        ArgumentNullException.ThrowIfNull(server);
        ArgumentNullException.ThrowIfNull(modes);
        ArgumentNullException.ThrowIfNull(baseDir);
        ArgumentNullException.ThrowIfNull(probe);

        var coreIssues = GetIssuesForBuiltInCore(server, modes, baseDir, probe);
        var modeIssues = modes.Count > 0 ? CollectModeIssues(modes, baseDir, probe) : Array.Empty<DependencyIssue>();

        if (coreIssues.Count == 0 && modeIssues.Count == 0)
            return;

        var issues = coreIssues.Concat(modeIssues).ToArray();
        var scopeTitle = ResolveScopeTitle(issues);
        throw new DependencyException(scopeTitle, issues);
    }

    public static string FormatDiagnostics(EnvironmentReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        var sb = new StringBuilder();
        sb.AppendLine($"{i18N.Translate("Core files")}: {(report.MissingCoreFiles.Count == 0 ? i18N.Translate("Complete") : string.Format(i18N.Translate("Missing {0}"), string.Join(", ", report.MissingCoreFiles)))}");
        sb.AppendLine($"{i18N.Translate("Process mode files")}: {(report.MissingProcessFiles.Count == 0 ? i18N.Translate("Complete") : string.Format(i18N.Translate("Missing {0}"), string.Join(", ", report.MissingProcessFiles)))}");
        sb.AppendLine($"{i18N.Translate("TUN mode files")}: {(report.MissingTunFiles.Count == 0 ? i18N.Translate("Complete") : string.Format(i18N.Translate("Missing {0}"), string.Join(", ", report.MissingTunFiles)))}");
        sb.AppendLine($"{i18N.Translate("Share mode files")}: {(report.MissingShareFiles.Count == 0 ? i18N.Translate("Complete") : string.Format(i18N.Translate("Missing {0}"), string.Join(", ", report.MissingShareFiles)))}");
        sb.AppendLine($"{i18N.Translate("BFE service")}: {FormatBfeStatus(report.IsBfeRunning)}");
        sb.AppendLine($"CPU AVX2 (x86-64-v3): {(report.SupportsAvx2V3 ? i18N.Translate("Supported") : i18N.Translate("Unsupported"))}");
        sb.AppendLine($"Npcap: {FormatNpcapStatus(report.Npcap)}");
        sb.Append($"VC++ 2015-2022 (x64): {(report.HasVcRedistX64 ? i18N.Translate("Installed") : i18N.Translate("Not installed"))}");
        return sb.ToString();
    }

    public static string FormatBfeStatus(bool? running) => running switch
    {
        true => i18N.Translate("Running"),
        false => i18N.Translate("Stopped"),
        null => i18N.Translate("Unknown")
    };

    public static string FormatNpcapStatus(NpcapStatus status) => status switch
    {
        NpcapStatus.Ready => i18N.Translate("WinPcap compatible"),
        NpcapStatus.MissingWinPcapCompat => i18N.Translate("WinPcap compatibility disabled"),
        _ => i18N.Translate("Not installed")
    };

    private static IReadOnlyList<DependencyIssue> CollectModeIssues(
        IReadOnlyList<Mode> modes,
        string baseDir,
        IEnvironmentProbe probe)
    {
        var issues = new List<DependencyIssue>();
        bool? isArm64 = null;
        var addedArm64 = false;

        bool IsArm64() => isArm64 ??= probe.IsArm64Emulated();

        var types = modes.Select(m => m.Type).Distinct().ToArray();
        foreach (var type in types)
        {
            switch (type)
            {
                case ModeType.ProcessMode:
                    if (IsArm64() && !addedArm64)
                    {
                        addedArm64 = true;
                        issues.Add(CreateArm64DriverIssue(DependencyScope.ProcessMode));
                    }

                    var missingProcess = CollectMissingFiles(baseDir, ProcessFiles, probe);
                    if (missingProcess.Count > 0)
                    {
                        issues.Add(new DependencyIssue(
                            "missing_process_files",
                            DependencyScope.ProcessMode,
                            i18N.Translate("Missing Process mode files"),
                            string.Format(
                                i18N.Translate("Missing: {0}. Extract the full release archive and check antivirus quarantine."),
                                string.Join(", ", missingProcess)),
                            DependencyActionKind.OpenDirectory,
                            i18N.Translate("Open directory"),
                            baseDir));
                    }

                    if (probe.IsServiceRunning("BFE") == false)
                    {
                        issues.Add(new DependencyIssue(
                            "bfe_service_stopped",
                            DependencyScope.ProcessMode,
                            i18N.Translate("Base Filtering Engine (BFE) service is stopped"),
                            i18N.Translate("Process mode requires the Windows Base Filtering Engine (BFE) service to be running."),
                            DependencyActionKind.OpenServices,
                            i18N.Translate("Open Services"),
                            "services.msc"));
                    }
                    break;

                case ModeType.TunMode:
                    if (IsArm64() && !addedArm64)
                    {
                        addedArm64 = true;
                        issues.Add(CreateArm64DriverIssue(DependencyScope.TunMode));
                    }

                    if (!probe.SupportsX8664V3())
                    {
                        issues.Add(new DependencyIssue(
                            "cpu_missing_avx2",
                            DependencyScope.TunMode,
                            i18N.Translate("CPU does not support AVX2 (x86-64-v3)"),
                            i18N.Translate("tun2socks-windows-amd64-v3.exe requires an AVX2-capable CPU. Use Process mode or Web proxy mode instead.")));
                    }

                    var missingTun = CollectMissingFiles(baseDir, TunFiles, probe);
                    if (missingTun.Count > 0)
                    {
                        issues.Add(new DependencyIssue(
                            "missing_tun_files",
                            DependencyScope.TunMode,
                            i18N.Translate("Missing TUN mode files"),
                            string.Format(
                                i18N.Translate("Missing: {0}. Extract the full release archive and check antivirus quarantine."),
                                string.Join(", ", missingTun)),
                            DependencyActionKind.OpenDirectory,
                            i18N.Translate("Open directory"),
                            baseDir));
                    }
                    break;

                case ModeType.ShareMode:
                    var missingShare = CollectMissingFiles(baseDir, ShareFiles, probe);
                    if (missingShare.Count > 0)
                    {
                        issues.Add(new DependencyIssue(
                            "missing_share_files",
                            DependencyScope.ShareMode,
                            i18N.Translate("Missing Share mode files"),
                            string.Format(
                                i18N.Translate("Missing: {0}. Extract the full release archive and check antivirus quarantine."),
                                string.Join(", ", missingShare)),
                            DependencyActionKind.OpenDirectory,
                            i18N.Translate("Open directory"),
                            baseDir));
                    }

                    var npcap = DetectNpcap(probe);
                    if (npcap == NpcapStatus.Missing)
                    {
                        issues.Add(new DependencyIssue(
                            "npcap_missing",
                            DependencyScope.ShareMode,
                            i18N.Translate("Npcap is not installed"),
                            i18N.Translate("Share mode requires Npcap. Check \"Install Npcap in WinPcap API-compatible Mode\" during installation."),
                            DependencyActionKind.OpenUrl,
                            i18N.Translate("Download Npcap"),
                            NpcapDownloadUrl));
                    }
                    else if (npcap == NpcapStatus.MissingWinPcapCompat)
                    {
                        issues.Add(new DependencyIssue(
                            "npcap_winpcap_compat",
                            DependencyScope.ShareMode,
                            i18N.Translate("Npcap WinPcap compatibility mode is disabled"),
                            i18N.Translate("Npcap is installed, but WinPcap API-compatible mode is not enabled. Reinstall Npcap with \"Install Npcap in WinPcap API-compatible Mode\" checked."),
                            DependencyActionKind.OpenUrl,
                            i18N.Translate("Download Npcap"),
                            NpcapDownloadUrl));
                    }

                    if (!probe.CanLoadNativeLibrary("vcruntime140.dll"))
                    {
                        issues.Add(new DependencyIssue(
                            "vcredist_x64_missing",
                            DependencyScope.ShareMode,
                            i18N.Translate("Missing Visual C++ 2015-2022 Redistributable (x64)"),
                            i18N.Translate("pcap2socks.exe requires VCRUNTIME140.dll from Microsoft Visual C++ 2015-2022 Redistributable (x64)."),
                            DependencyActionKind.OpenUrl,
                            i18N.Translate("Download runtime"),
                            VcRedistX64DownloadUrl));
                    }
                    break;
            }
        }

        return issues;
    }

    private static DependencyIssue CreateArm64DriverIssue(DependencyScope scope) => new(
        "arm64_driver_unsupported",
        scope,
        i18N.Translate("x64 kernel drivers are not supported on ARM64 Windows"),
        i18N.Translate("Process mode and TUN mode rely on x64 drivers that cannot load under ARM64 emulation. Use Web proxy mode instead."));

    private static NpcapStatus DetectNpcap(IEnvironmentProbe probe)
    {
        if (probe.CanLoadNativeLibrary("packet.dll"))
            return NpcapStatus.Ready;

        var npcapPacketPath = Path.Combine(probe.SystemDirectory, "Npcap", "Packet.dll");
        return probe.FileExists(npcapPacketPath)
            ? NpcapStatus.MissingWinPcapCompat
            : NpcapStatus.Missing;
    }

    private static IReadOnlyList<string> CollectMissingFiles(string baseDir, IEnumerable<string> relativePaths, IEnvironmentProbe probe)
    {
        var missing = new List<string>();
        foreach (var file in relativePaths)
        {
            if (!probe.FileExists(Path.Combine(baseDir, file)))
                missing.Add(file);
        }
        return missing;
    }

    private static string ResolveScopeTitle(IReadOnlyList<DependencyIssue> issues)
    {
        var labels = issues
            .Select(issue => issue.Scope switch
            {
                DependencyScope.ProcessMode => i18N.Translate("Process mode"),
                DependencyScope.TunMode => i18N.Translate("TUN mode"),
                DependencyScope.ShareMode => i18N.Translate("Share mode"),
                _ => null
            })
            .Where(label => label != null)
            .Distinct()
            .ToArray();

        return labels.Length > 0 ? string.Join(" + ", labels) : i18N.Translate("Core components");
    }

    private static string NormalizeBaseDir(string? baseDir) =>
        string.IsNullOrWhiteSpace(baseDir) ? Global.NetchDir : Path.TrimEndingDirectorySeparator(baseDir);
}
