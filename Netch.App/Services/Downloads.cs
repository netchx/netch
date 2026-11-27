using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Netch.Controllers;
using Netch.Enums;
using Netch.Models.GitHubRelease;
using Netch.Models.Modes.WebProxyMode;
using Netch.Services;
using Netch.Utils;

namespace Netch.App.Services;

internal enum ModeOrigin { Bundled, Update, Modified, Custom }

internal enum RuleChangeKind { Added, Updated, Conflict }
internal enum ConflictAction { Keep, Replace, Copy }

internal sealed record RuleChange(string Path, string Sha, string? LocalSha, RuleChangeKind Kind);

internal sealed record CorePackage(string Name, string Repo, string File, string VersionArgument, Func<string, string> Asset, string? ArchiveFile = null);
internal sealed record UpdateChoice(string Version, string Url, string ReleaseUrl)
{
    public bool Prerelease { get; init; }
}
internal readonly record struct TransferProgress(double Percent, ulong BytesPerSecond);
internal sealed record DownloadEntry(DateTime Time, string Name, string Version, bool Success, string? Message = null);

internal sealed class DownloadTransfer(CorePackage? core, PacRulePreset? pacPreset = null)
{
    private TaskCompletionSource? _paused;

    public CorePackage? Core { get; } = core;
    public PacRulePreset? PacPreset { get; } = pacPreset;
    public bool IsApplication => Core == null && PacPreset == null;
    public TransferProgress Progress { get; set; } = new(-1, 0);
    public bool IsPaused => _paused != null;
    public bool CanPause { get; private set; } = true;

    public void TogglePause()
    {
        if (!CanPause)
            return;
        if (_paused == null)
            _paused = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        else
        {
            var paused = _paused;
            _paused = null;
            paused.TrySetResult();
        }
    }

    public Task WaitWhilePausedAsync() => _paused?.Task ?? Task.CompletedTask;

    public void FinishReceiving() => CanPause = false;
}

internal static partial class Downloads
{
    public static readonly CorePackage[] Cores =
    [
        new("sing-box", "SagerNet/sing-box", @"sing_box\sing-box.exe", "version", v => $"sing-box-{v}-windows-amd64.zip"),
        new("Xray-core", "XTLS/Xray-core", "xray.exe", "version", _ => "Xray-windows-64.zip"),
        new("Mihomo", "MetaCubeX/mihomo", "mihomo.exe", "-v", v => $"mihomo-windows-amd64-v1-v{v}.zip", "mihomo-windows-amd64-v1.exe"),
        new("tun2socks", "xjasonlyu/tun2socks", "tun2socks-windows-amd64-v3.exe", "--version", _ => "tun2socks-windows-amd64-v3.zip"),
        new("WinTUN", "", "wintun.dll", "", v => $"wintun-{v}.zip", "wintun/bin/amd64/wintun.dll"),
        new("pcap2socks", "zhxie/pcap2socks", "pcap2socks.exe", "--version", v => $"pcap2socks-v{v}-windows-amd64.zip")
    ];

    public static event Action? Changed;
    public static event Action? ProgressChanged;
    public static DownloadTransfer? ActiveTransfer { get; private set; }
    public static Dictionary<CorePackage, List<UpdateChoice>> Versions { get; } = [];
    public static Dictionary<CorePackage, string> Installed { get; } = [];
    public static ConcurrentDictionary<string, string> Errors { get; } = [];
    public static List<RuleChange> Rules { get; private set; } = [];
    public static int RemoteRuleCount { get; private set; }
    public static string RemoteTree { get; private set; } = "";
    public static DateTime? LastChecked { get; private set; }
    public static DateTimeOffset? LastRulesSynchronized => _manifest.Source == SourceKey ? _manifest.SynchronizedAt : null;
    public static string? RemotePacVersion { get; private set; }
    public static string RemotePacReleaseUrl => string.IsNullOrWhiteSpace(RemotePacVersion)
        ? PacRuleService.ReleasesUrl
        : $"{PacRuleService.ReleasesUrl}/tag/{RemotePacVersion}";
    public static DateTimeOffset? LastPacSynchronized => _pacManifest.SynchronizedAt;
    public static Release? AppRelease { get; private set; }
    public static Asset? AppAsset { get; private set; }
    public static DateTimeOffset? RateLimitResetUtc { get; private set; }
    public static bool IsApiRateLimited => RateLimitResetUtc is { } reset && reset > DateTimeOffset.UtcNow;
    public static List<DownloadEntry> History { get; } = ReadJson<List<DownloadEntry>>(HistoryPath) ?? [];
    public static bool Checking { get; private set; }
    public static bool Applying { get; private set; }
    public static int PendingPacCount => PacRuleService.RemotePresets.Count(IsPacUpdateAvailable);
    public static int PendingCount => Rules.Count + Cores.Count(core => AvailableCore(core) != null) + PendingPacCount;
    private static readonly Dictionary<CorePackage, DateTime> CoreChecked = [];
    private static readonly object CacheLock = new();
    private static readonly UpdateCacheStore CacheStore = new(Path.Combine(Configuration.DataDirectoryFullName, "update-cache.json"));
    private static UpdateCacheState _cache = new();
    private static bool _cacheLoaded;
    private sealed record ApiFetchResult(string? Body, bool FromCache, bool NotModified, string? RateLimitError);
    private static string HistoryPath => Path.Combine(Configuration.DataDirectoryFullName, "downloads.json");
    private static string BaseRulesPath => Path.Combine(ModeService.Instance.ModeDirectoryFullName, ModeService.ManifestFileName);
    private static string PacManifestPath => Path.Combine(PacRuleService.GetUserRuleDirectory(), ModeService.ManifestFileName);
    private static string SourceKey => $"{Global.Settings.ModeRepository}/{Global.Settings.ModeBranch}/{Global.Settings.ModeRepositoryPath}";
    private static RuleManifest _manifest = ReadJson<RuleManifest>(BaseRulesPath) ?? new();
    private static PacManifest _pacManifest = ReadJson<PacManifest>(PacManifestPath) ?? new();
    private sealed record RuleManifest
    {
        public string? Source { get; set; }
        public DateTimeOffset? SynchronizedAt { get; set; }
        public Dictionary<string, string> Blobs { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    }

    private sealed record PacManifest
    {
        public DateTimeOffset? SynchronizedAt { get; set; }
        public Dictionary<string, string> Versions { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    }

    public static string? FormatRateLimitError()
    {
        if (RateLimitResetUtc is not { } reset || reset <= DateTimeOffset.UtcNow)
            return null;

        return Helpers.Ui.TF("GitHub API rate limit reached; resets at {0:HH:mm}.",
            "已触发 GitHub API 速率限制，预计 {0:HH:mm} 恢复。", reset.ToLocalTime());
    }

    private static void EnsureCacheLoaded()
    {
        lock (CacheLock)
        {
            if (_cacheLoaded)
                return;

            _cache = CacheStore.Load();
            _cacheLoaded = true;

            if (_cache.RateLimitResetUtc is { } reset && reset > DateTimeOffset.UtcNow)
                RateLimitResetUtc = reset;
            else
            {
                RateLimitResetUtc = null;
                _cache.RateLimitResetUtc = null;
            }

            if (_cache.LastCheckedUtc is { } lastChecked)
                LastChecked = lastChecked.LocalDateTime;

            if (!string.IsNullOrWhiteSpace(_cache.RemotePacVersion))
                RemotePacVersion = _cache.RemotePacVersion;

            if (_cache.AppRelease is { } cachedApp &&
                !string.IsNullOrWhiteSpace(cachedApp.TagName) &&
                (!cachedApp.Prerelease || Global.Settings.CheckBetaUpdate))
            {
                AppRelease = new Release
                {
                    tag_name = cachedApp.TagName,
                    html_url = cachedApp.HtmlUrl,
                    prerelease = cachedApp.Prerelease
                };
                AppAsset = !string.IsNullOrWhiteSpace(cachedApp.AssetName) && !string.IsNullOrWhiteSpace(cachedApp.AssetDownloadUrl)
                    ? new Asset { name = cachedApp.AssetName, browser_download_url = cachedApp.AssetDownloadUrl }
                    : null;
            }

            foreach (var core in Cores)
            {
                var restored = new List<UpdateChoice>();
                if (_cache.CoreLatest.TryGetValue(core.Name, out var latestEntry))
                {
                    restored.Add(FromCachedEntry(latestEntry));
                    CoreChecked[core] = latestEntry.CheckedAtUtc.LocalDateTime;
                }
                if (_cache.CoreHistory.TryGetValue(core.Name, out var history))
                {
                    foreach (var entry in history.Entries)
                        restored.Add(FromCachedEntry(entry));
                    if (!CoreChecked.ContainsKey(core))
                        CoreChecked[core] = history.FetchedAtUtc.LocalDateTime;
                }
                if (restored.Count > 0)
                    Versions[core] = MergeChoices(restored);
            }

            if (string.Equals(_cache.RulesSourceKey, SourceKey, StringComparison.OrdinalIgnoreCase) &&
                _cache.ApiResponses.TryGetValue(RulesTreeApiUrl(), out var cachedTree) &&
                !string.IsNullOrWhiteSpace(cachedTree.Body))
            {
                try { ApplyRulesTreeJson(cachedTree.Body); }
                catch (Exception ex) { Log.Debug(ex, "Restore cached rules tree failed"); }
            }
        }
    }

    private static void SaveCache()
    {
        lock (CacheLock)
            CacheStore.Save(_cache);
    }

    private static UpdateChoice FromCachedEntry(CachedUpdateEntry entry) =>
        new(entry.Version, entry.Url, entry.ReleaseUrl) { Prerelease = entry.Prerelease };

    private static CachedUpdateEntry ToCachedEntry(UpdateChoice choice, DateTimeOffset checkedAtUtc) =>
        new()
        {
            Version = choice.Version,
            Url = choice.Url,
            ReleaseUrl = choice.ReleaseUrl,
            Prerelease = choice.Prerelease,
            CheckedAtUtc = checkedAtUtc
        };

    private static List<UpdateChoice> MergeChoices(IEnumerable<UpdateChoice> choices) =>
        choices
            .DistinctBy(c => c.Version, StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => c.Prerelease)
            .ThenByDescending(c => c.Version, new VersionUtil.VersionComparer())
            .ToList();

    public static async Task CheckOnStartupAsync()
    {
        EnsureCacheLoaded();
        await PopulateInstalledAsync();
        Changed?.Invoke();

        DateTimeOffset? lastAuto;
        lock (CacheLock)
            lastAuto = _cache.LastAutoCheckUtc;

        if (!GitHubRateLimitPolicy.ShouldRunAutoCheck(Global.Settings.AutoCheckUpdateIntervalHours, lastAuto, DateTimeOffset.UtcNow))
            return;

        if (Global.Settings.DownloadChannel == DownloadChannel.Proxy && MainController.Socks5Server == null)
            return;

        await CheckAsync(isAuto: true);
    }

    public static Task CheckAsync() => CheckAsync(isAuto: false);

    public static async Task CheckAsync(bool isAuto)
    {
        EnsureCacheLoaded();
        if (Checking || Applying)
            return;
        Checking = true;
        Errors.Clear();
        Changed?.Invoke();
        try
        {
            var results = await Task.WhenAll(Cores.Select(async core =>
            {
                var version = await ReadVersionAsync(core);
                try { return (Core: core, Version: version, Result: await FetchCoreAsync(core), Error: (string?)null); }
                catch (Exception e) { return (Core: core, Version: version, Result: ((List<UpdateChoice>? Choices, string? Warning)?)null, Error: (string?)e.Message); }
            }));

            var nowUtc = DateTimeOffset.UtcNow;
            foreach (var result in results)
            {
                Installed[result.Core] = result.Version;
                if (result.Result is { Choices: { } choices })
                {
                    Versions[result.Core] = choices;
                    CoreChecked[result.Core] = DateTime.Now;
                    var latestStable = choices.FirstOrDefault(c => !c.Prerelease);
                    if (latestStable != null)
                    {
                        lock (CacheLock)
                            _cache.CoreLatest[result.Core.Name] = ToCachedEntry(latestStable, nowUtc);
                    }
                    if (!string.IsNullOrWhiteSpace(result.Result.Value.Warning))
                        Errors[result.Core.Name] = result.Result.Value.Warning;
                }
                else if (result.Error != null)
                {
                    Errors[result.Core.Name] = CoreChecked.TryGetValue(result.Core, out var last)
                        ? Helpers.Ui.TF("Last verified {0:g}. {1}", "上次确认于 {0:g}。{1}", last, result.Error)
                        : Helpers.Ui.TF("Version unavailable. {0}", "暂无法确认版本。{0}", result.Error);
                }
            }
            var rulesTask = CheckRulesAsync();
            var appTask = CheckApplicationAsync();
            var pacTask = CheckPacRulesAsync();
            await Task.WhenAll(rulesTask, appTask, pacTask);
            var anyCoreOk = results.Any(r => r.Result is { Choices: not null });
            var anyDataSucceeded = anyCoreOk || await rulesTask || await appTask || await pacTask;
            if (anyDataSucceeded)
            {
                LastChecked = DateTime.Now;
                lock (CacheLock)
                {
                    _cache.LastCheckedUtc = nowUtc;
                    _cache.LastAutoCheckUtc = nowUtc;
                    _cache.RemotePacVersion = RemotePacVersion;
                }
                SaveCache();
            }
            else if (isAuto && IsApiRateLimited)
            {
                lock (CacheLock)
                    _cache.LastAutoCheckUtc = nowUtc;
                SaveCache();
            }
        }
        finally
        {
            Checking = false;
            Changed?.Invoke();
        }
    }

    private static async Task<bool> CheckApplicationAsync()
    {
        try
        {
            if (!Global.Settings.CheckBetaUpdate)
            {
                return await FetchLatestApplicationAsync();
            }

            var url = $"https://api.github.com/repos/{UpdateChecker.Owner}/{UpdateChecker.Repo}/releases?per_page=20";
            var apiResult = await FetchApiCachedAsync(url);
            if (apiResult.Body != null)
            {
                var releases = (JsonSerializer.Deserialize<List<Release>>(apiResult.Body) ?? [])
                    .Where(r => !r.draft && (Global.Settings.CheckBetaUpdate || !r.prerelease))
                    .ToList();
                AppRelease = releases.FirstOrDefault();
                AppAsset = UpdateChecker.SelectReleaseAsset(AppRelease?.assets, UpdateChecker.IsSelfContained);
                if (AppRelease != null && IsNewer(AppRelease.tag_name, UpdateChecker.Version) && AppAsset == null)
                    Errors[UpdateChecker.Name] = Helpers.Ui.T("This release has no WinUI x64 ZIP.", "该版本未提供 WinUI x64 ZIP。");

                if (AppRelease != null)
                {
                    lock (CacheLock)
                    {
                        _cache.AppRelease = new CachedAppReleaseEntry
                        {
                            TagName = AppRelease.tag_name ?? string.Empty,
                            HtmlUrl = AppRelease.html_url ?? string.Empty,
                            Prerelease = AppRelease.prerelease,
                            AssetName = AppAsset?.name,
                            AssetDownloadUrl = AppAsset?.browser_download_url,
                            CheckedAtUtc = DateTimeOffset.UtcNow
                        };
                    }
                }
                if (apiResult.RateLimitError != null)
                    Errors[UpdateChecker.Name] = apiResult.RateLimitError;
                return true;
            }

            var ok = await FetchLatestApplicationAsync();
            if (apiResult.RateLimitError != null && !Errors.ContainsKey(UpdateChecker.Name))
                Errors[UpdateChecker.Name] = apiResult.RateLimitError;
            return ok;
        }
        catch (Exception e)
        {
            Errors[UpdateChecker.Name] = e.Message;
            return false;
        }
    }

    private static async Task<bool> FetchLatestApplicationAsync()
    {
        using var client = CreateClient();
        var prefix = $"https://github.com/{UpdateChecker.Owner}/{UpdateChecker.Repo}/releases";
        using var page = await client.GetAsync(Request(prefix + "/latest"), HttpCompletionOption.ResponseHeadersRead);
        if (page.StatusCode == HttpStatusCode.NotFound)
        {
            AppRelease = null;
            AppAsset = null;
            lock (CacheLock)
                _cache.AppRelease = null;
            return true;
        }
        page.EnsureSuccessStatusCode();
        var finalUri = page.RequestMessage?.RequestUri;
        var tag = finalUri?.Segments.LastOrDefault()?.Trim('/');
        if (finalUri == null || string.IsNullOrWhiteSpace(tag)
            || !finalUri.AbsolutePath.TrimEnd('/').EndsWith($"/{UpdateChecker.Owner}/{UpdateChecker.Repo}/releases/tag/{tag}", StringComparison.OrdinalIgnoreCase))
        {
            // Repository may only contain prereleases right now.
            AppRelease = null;
            AppAsset = null;
            lock (CacheLock)
                _cache.AppRelease = null;
            return true;
        }

        Asset? resolvedAsset = null;
        if (IsNewer(tag, UpdateChecker.Version))
        {
            var candidates = GitHubRateLimitPolicy.GetApplicationAssetCandidates(tag, UpdateChecker.IsSelfContained);
            foreach (var candidateName in candidates)
            {
                var downloadUrl = $"{prefix}/download/{tag}/{candidateName}";
                if (await TryVerifyAssetAsync(client, downloadUrl))
                {
                    resolvedAsset = new Asset { name = candidateName, browser_download_url = downloadUrl };
                    break;
                }
            }
        }

        AppRelease = new Release
        {
            tag_name = tag,
            html_url = $"{prefix}/tag/{tag}",
            prerelease = false
        };
        AppAsset = resolvedAsset;

        if (IsNewer(AppRelease.tag_name, UpdateChecker.Version) && AppAsset == null)
            Errors[UpdateChecker.Name] = Helpers.Ui.T("This release has no WinUI x64 ZIP.", "该版本未提供 WinUI x64 ZIP。");

        lock (CacheLock)
        {
            _cache.AppRelease = new CachedAppReleaseEntry
            {
                TagName = AppRelease.tag_name,
                HtmlUrl = AppRelease.html_url,
                Prerelease = false,
                AssetName = AppAsset?.name,
                AssetDownloadUrl = AppAsset?.browser_download_url,
                CheckedAtUtc = DateTimeOffset.UtcNow
            };
        }
        return true;
    }

    public static bool IsNewer(string available, string installed)
    {
        if (available.Length == 0)
            return false;
        if (installed.Length == 0)
            return true;
        return VersionUtil.CompareVersion(available.TrimStart('v', 'V'), installed.TrimStart('v', 'V')) > 0;
    }

    public static string? InstalledPacVersion(PacRulePreset preset) =>
        InstalledPacVersion(preset, PacRuleService.GetRuleStatus(preset, includeRuleCount: false));

    public static string? InstalledPacVersion(PacRulePreset preset, PacRuleStatus status)
    {
        if (PacRuleService.GetFileName(preset) is not { } file)
            return null;

        return status.IsUserUpdated && _pacManifest.Versions.TryGetValue(file, out var version) ? version : null;
    }

    public static bool IsPacUpdateAvailable(PacRulePreset preset)
    {
        var status = PacRuleService.GetRuleStatus(preset, includeRuleCount: false);
        return PacRuleService.IsUpdateAvailable(status, InstalledPacVersion(preset, status), RemotePacVersion);
    }

    private static async Task<bool> CheckPacRulesAsync()
    {
        try
        {
            using var client = CreateClient();
            using var response = await client.GetAsync(Request($"{PacRuleService.ReleasesUrl}/latest"), HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            var finalUri = response.RequestMessage?.RequestUri;
            var tag = finalUri?.Segments.LastOrDefault()?.Trim('/');
            if (finalUri == null || string.IsNullOrWhiteSpace(tag)
                || !finalUri.AbsolutePath.TrimEnd('/').EndsWith($"/{PacRuleService.Repository}/releases/tag/{tag}", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("GitHub did not resolve the latest PAC release tag.");
            }

            RemotePacVersion = tag;
            return true;
        }
        catch (Exception e)
        {
            Errors["PAC"] = e.Message;
            return false;
        }
    }

    public static UpdateChoice? LatestStable(CorePackage core) =>
        Versions.GetValueOrDefault(core)?.FirstOrDefault(choice => !choice.Prerelease);

    public static UpdateChoice? AvailableCore(CorePackage core)
    {
        var latest = LatestStable(core);
        var installed = Installed.GetValueOrDefault(core) ?? "";
        return latest != null && !(Global.Settings.CheckBetaUpdate && installed.StartsWith("alpha-", StringComparison.Ordinal))
            && IsNewer(latest.Version, installed) ? latest : null;
    }

    public static async Task PopulateInstalledAsync()
    {
        EnsureCacheLoaded();
        var pending = Cores.Where(core => !Installed.ContainsKey(core)).ToArray();
        var values = await Task.WhenAll(pending.Select(ReadVersionAsync));
        for (var i = 0; i < pending.Length; i++)
            Installed.TryAdd(pending[i], values[i]);
        Changed?.Invoke();
    }

    private static async Task<string> ReadVersionAsync(CorePackage core)
    {
        var path = Path.Combine(Bootstrap.BinDirectory, core.File);
        if (!File.Exists(path))
            return "";
        if (core.VersionArgument.Length == 0)
            return Utils.Utils.GetFileVersion(path);
        try
        {
            using var process = Process.Start(new ProcessStartInfo(path, core.VersionArgument)
                { UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden });
            if (process == null)
                return "";
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            string? version;
            try
            {
                version = await process.StandardOutput.ReadLineAsync(timeout.Token);
                await process.WaitForExitAsync(timeout.Token);
            }
            catch (OperationCanceledException)
            {
                if (!process.HasExited)
                    process.Kill();
                return "";
            }
            return process.ExitCode == 0 ? VersionNumber().Match(version ?? "").Value : "";
        }
        catch (Exception e)
        {
            Log.Debug(e, "Read {Name} version", core.Name);
            return "";
        }
    }

    [GeneratedRegex(@"\balpha-[0-9a-f]{7,40}\b|\d+\.\d+\.\d+(?:-[A-Za-z]+\.?\d*)?")]
    private static partial Regex VersionNumber();

    [GeneratedRegex(@"^mihomo-windows-amd64-v1-(alpha-[0-9a-f]{7,40})\.zip$")]
    private static partial Regex MihomoAlphaAsset();

    private static async Task<(List<UpdateChoice> Choices, string? Warning)> FetchCoreAsync(CorePackage core)
    {
        if (core.Repo.Length == 0)
        {
            using var client = CreateClient();
            var page = await client.GetStringAsync("https://www.wintun.net/");
            var list = Regex.Matches(page, @"(?:https://www\.wintun\.net)?/?builds/wintun-(?<version>\d+\.\d+\.\d+)\.zip")
                .Select(match => match.Groups["version"].Value).Distinct()
                .OrderByDescending(version => Version.Parse(version))
                .Select(version => new UpdateChoice(version, $"https://www.wintun.net/builds/{core.Asset(version)}", "https://www.wintun.net/"))
                .ToList();
            return (list, null);
        }

        if (!Global.Settings.CheckBetaUpdate)
        {
            var latest = await FetchLatestCoreAsync(core);
            var existing = Versions.GetValueOrDefault(core) ?? [];
            return (MergeChoices([latest, .. existing]), null);
        }

        var apiUrl = $"https://api.github.com/repos/{core.Repo}/releases?per_page=20";
        var apiResult = await FetchApiCachedAsync(apiUrl);
        if (apiResult.Body != null && apiResult.RateLimitError == null)
        {
            var releases = (JsonSerializer.Deserialize<List<Release>>(apiResult.Body) ?? [])
                .Where(r => !r.draft && (Global.Settings.CheckBetaUpdate || !r.prerelease))
                .ToList();
            var choices = SelectCoreChoices(core, releases);
            if (!choices.Any(choice => !choice.Prerelease))
                choices.Insert(0, await FetchLatestCoreAsync(core));
            var now = DateTimeOffset.UtcNow;
            lock (CacheLock)
            {
                _cache.CoreHistory[core.Name] = new CachedCoreHistory
                {
                    IncludePrerelease = Global.Settings.CheckBetaUpdate,
                    FetchedAtUtc = now,
                    Entries = choices.Select(c => ToCachedEntry(c, now)).ToList()
                };
            }
            return (choices, null);
        }

        var fallbackLatest = await FetchLatestCoreAsync(core);
        var cached = Versions.GetValueOrDefault(core) ?? [];
        if (apiResult.Body != null)
        {
            var cachedReleases = (JsonSerializer.Deserialize<List<Release>>(apiResult.Body) ?? [])
                .Where(r => !r.draft && (Global.Settings.CheckBetaUpdate || !r.prerelease))
                .ToList();
            cached = SelectCoreChoices(core, cachedReleases);
        }
        if (core.Name == "Mihomo" && Global.Settings.CheckBetaUpdate)
        {
            var alpha = await FetchMihomoAlphaAsync(core);
            if (alpha != null)
                cached = cached.Where(choice => !choice.Version.StartsWith("alpha-", StringComparison.Ordinal))
                    .Append(alpha).ToList();
        }
        return (MergeChoices([fallbackLatest, .. cached]), apiResult.RateLimitError);
    }

    public static async Task<List<UpdateChoice>> LoadCoreHistoryAsync(CorePackage core)
    {
        EnsureCacheLoaded();
        var existing = (Versions.GetValueOrDefault(core) ?? [])
            .Where(c => !c.Prerelease || Global.Settings.CheckBetaUpdate)
            .ToArray();
        if (core.Repo.Length == 0)
        {
            if (Versions.TryGetValue(core, out var existingWinTun) && existingWinTun.Count > 0)
                return existingWinTun;

            var fetched = (await FetchCoreAsync(core)).Choices;
            Versions[core] = fetched;
            Changed?.Invoke();
            return fetched;
        }

        CachedCoreHistory? cachedHistory;
        lock (CacheLock)
            _cache.CoreHistory.TryGetValue(core.Name, out cachedHistory);

        if (cachedHistory != null &&
            cachedHistory.IncludePrerelease == Global.Settings.CheckBetaUpdate &&
            DateTimeOffset.UtcNow - cachedHistory.FetchedAtUtc < TimeSpan.FromHours(1))
        {
            var fresh = MergeChoices(existing.Concat(cachedHistory.Entries.Select(FromCachedEntry)));
            Versions[core] = fresh;
            return fresh;
        }

        if (IsApiRateLimited)
        {
            if (cachedHistory != null && cachedHistory.Entries.Count > 0)
            {
                var merged = MergeChoices(existing.Concat(
                    cachedHistory.Entries.Select(FromCachedEntry)
                        .Where(c => !c.Prerelease || Global.Settings.CheckBetaUpdate)));
                Versions[core] = merged;
                return merged;
            }

            if (existing.Length > 1)
                return existing.ToList();

            throw new InvalidOperationException(FormatRateLimitError() ?? "GitHub API rate limit reached.");
        }

        var apiUrl = $"https://api.github.com/repos/{core.Repo}/releases?per_page=20";
        var apiResult = await FetchApiCachedAsync(apiUrl);
        if (apiResult.Body == null)
            throw new InvalidOperationException(apiResult.RateLimitError ?? Helpers.Ui.T("Could not load version history.", "无法加载历史版本。"));

        var releases = (JsonSerializer.Deserialize<List<Release>>(apiResult.Body) ?? [])
            .Where(r => !r.draft && (Global.Settings.CheckBetaUpdate || !r.prerelease))
            .ToList();
        var choices = SelectCoreChoices(core, releases);
        if (!choices.Any(choice => !choice.Prerelease))
        {
            try { choices.Insert(0, await FetchLatestCoreAsync(core)); }
            catch (Exception ex) { Log.Debug(ex, "Fallback latest release lookup for {Core} failed", core.Name); }
        }
        choices = MergeChoices(existing.Concat(choices));

        var now = DateTimeOffset.UtcNow;
        lock (CacheLock)
        {
            _cache.CoreHistory[core.Name] = new CachedCoreHistory
            {
                IncludePrerelease = Global.Settings.CheckBetaUpdate,
                FetchedAtUtc = now,
                Entries = choices.Select(c => ToCachedEntry(c, now)).ToList()
            };
        }
        SaveCache();
        Versions[core] = choices;
        Changed?.Invoke();
        return choices;
    }

    private static List<UpdateChoice> SelectCoreChoices(CorePackage core, List<Release> releases) =>
        releases.Select(release =>
        {
            var version = release.tag_name.TrimStart('v', 'V');
            if (core.Name == "Mihomo" && release.prerelease && release.tag_name == "Prerelease-Alpha")
            {
                var alpha = release.assets?.Where(a => MihomoAlphaAsset().IsMatch(a.name))
                    .OrderByDescending(a => a.updated_at).FirstOrDefault();
                return alpha == null ? null : new UpdateChoice(MihomoAlphaAsset().Match(alpha.name).Groups[1].Value,
                    alpha.browser_download_url, release.html_url) { Prerelease = true };
            }
            var name = core.Asset(version);
            var asset = release.assets?.FirstOrDefault(a => string.Equals(a.name, name, StringComparison.OrdinalIgnoreCase));
            return asset == null ? null : new UpdateChoice(version, asset.browser_download_url, release.html_url)
                { Prerelease = release.prerelease };
        }).OfType<UpdateChoice>().OrderBy(choice => choice.Prerelease)
            .ThenByDescending(choice => choice.Version, new VersionUtil.VersionComparer()).ToList();

    private static async Task<UpdateChoice> FetchLatestCoreAsync(CorePackage core)
    {
        using var client = CreateClient();
        var prefix = $"https://github.com/{core.Repo}/releases";
        using var page = await client.GetAsync(Request(prefix + "/latest"), HttpCompletionOption.ResponseHeadersRead);
        page.EnsureSuccessStatusCode();
        var release = page.RequestMessage?.RequestUri;
        var tag = release?.Segments.LastOrDefault()?.Trim('/');
        if (release == null || string.IsNullOrWhiteSpace(tag)
            || !release.AbsolutePath.TrimEnd('/').EndsWith($"/{core.Repo}/releases/tag/{tag}", StringComparison.OrdinalIgnoreCase)
            || (!SuffixVersion.TryParse(tag.TrimStart('v', 'V'), out _) && !Version.TryParse(tag.TrimStart('v', 'V'), out _)))
            throw new InvalidDataException("GitHub did not resolve the latest release tag.");

        var version = tag.TrimStart('v', 'V');
        var url = $"{prefix}/download/{tag}/{core.Asset(version)}";
        await VerifyAssetAsync(client, url);
        return new UpdateChoice(version, url, $"{prefix}/tag/{tag}");
    }

    private static async Task<UpdateChoice?> FetchMihomoAlphaAsync(CorePackage core)
    {
        using var client = CreateClient();
        var release = $"https://github.com/{core.Repo}/releases";
        var files = release + "/download/Prerelease-Alpha/";
        try
        {
            var version = (await client.GetStringAsync(Request(files + "version.txt"))).Trim();
            var name = $"mihomo-windows-amd64-v1-{version}.zip";
            if (!MihomoAlphaAsset().IsMatch(name))
                return null;
            var url = files + name;
            await VerifyAssetAsync(client, url);
            return new UpdateChoice(version, url, release + "/tag/Prerelease-Alpha") { Prerelease = true };
        }
        catch (Exception e) when (e is HttpRequestException or OperationCanceledException)
        {
            Log.Warning(e, "Discover Mihomo Alpha asset");
            return null;
        }
    }

    private static async Task VerifyAssetAsync(HttpClient client, string url)
    {
        if (!await TryVerifyAssetAsync(client, url))
            throw new HttpRequestException($"Release asset not accessible: {url}", null, HttpStatusCode.NotFound);
    }

    private static async Task<bool> TryVerifyAssetAsync(HttpClient client, string url)
    {
        var target = Request(url);
        using var request = new HttpRequestMessage(HttpMethod.Head, target);
        using var asset = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        if (asset.IsSuccessStatusCode)
            return true;

        if (asset.StatusCode == HttpStatusCode.MethodNotAllowed)
        {
            using var getReq = new HttpRequestMessage(HttpMethod.Get, target);
            getReq.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(0, 0);
            using var getResp = await client.SendAsync(getReq, HttpCompletionOption.ResponseHeadersRead);
            return getResp.IsSuccessStatusCode;
        }

        return false;
    }

    private static async Task<ApiFetchResult> FetchApiCachedAsync(string url)
    {
        CachedApiResponse? cached;
        lock (CacheLock)
            _cache.ApiResponses.TryGetValue(url, out cached);

        if (IsApiRateLimited)
            return new ApiFetchResult(cached?.Body, FromCache: cached != null, NotModified: false, FormatRateLimitError());

        using var client = CreateClient(isGitHubApi: true);
        using var request = new HttpRequestMessage(HttpMethod.Get, Request(url));
        if (cached != null && !string.IsNullOrWhiteSpace(cached.ETag))
            request.Headers.TryAddWithoutValidation("If-None-Match", cached.ETag);

        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead);
        var nowUtc = DateTimeOffset.UtcNow;
        if (GitHubRateLimitPolicy.TryParseRateLimitReset(response.StatusCode, response.Headers, nowUtc, out var resetUtc))
        {
            lock (CacheLock)
            {
                _cache.RateLimitResetUtc = resetUtc;
                RateLimitResetUtc = resetUtc;
            }
            SaveCache();
        }

        if (response.StatusCode == HttpStatusCode.NotModified && cached != null)
        {
            lock (CacheLock)
                cached.FetchedAtUtc = nowUtc;
            SaveCache();
            return new ApiFetchResult(cached.Body, FromCache: true, NotModified: true, null);
        }

        if (response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.TooManyRequests)
        {
            var err = FormatRateLimitError() ?? $"GitHub API rate limit reached (HTTP {(int)response.StatusCode}).";
            return new ApiFetchResult(cached?.Body, FromCache: cached != null, NotModified: false, err);
        }

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        var etag = response.Headers.ETag?.ToString();
        if (string.IsNullOrWhiteSpace(etag) && response.Headers.TryGetValues("ETag", out var etags))
            etag = etags.FirstOrDefault();

        lock (CacheLock)
        {
            _cache.ApiResponses[url] = new CachedApiResponse
            {
                ETag = etag,
                Body = body,
                FetchedAtUtc = nowUtc
            };
        }
        SaveCache();
        return new ApiFetchResult(body, FromCache: false, NotModified: false, null);
    }

    private static HttpClient CreateClient(bool isGitHubApi = false)
    {
        string? proxy = null;
        if (Global.Settings.DownloadChannel == DownloadChannel.Proxy)
        {
            var server = MainController.Socks5Server;
            if (server == null)
                throw new InvalidOperationException(Helpers.Ui.T("Connect a scene to use the current proxy.", "请先连接场景，再通过当前代理下载。"));
            proxy = WebUtil.BuildSocks5ProxyUri(server);
        }
        var client = WebUtil.CreateHttpClient(proxyServer: proxy);
        if (isGitHubApi)
        {
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
            client.DefaultRequestHeaders.TryAddWithoutValidation("X-GitHub-Api-Version", "2022-11-28");
        }
        return client;
    }

    private static string Request(string url)
    {
        if (Global.Settings.DownloadChannel != DownloadChannel.GitHubMirror || string.IsNullOrWhiteSpace(Global.Settings.GitHubMirror))
            return url;
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Host is "github.com" or "api.github.com" or "raw.githubusercontent.com"
            ? $"{Global.Settings.GitHubMirror.TrimEnd('/')}/{url}" : url;
    }

    private static string RuleUrl(string path) => "https://raw.githubusercontent.com/" +
        string.Join('/', Global.Settings.ModeRepository.Split('/').Concat([Global.Settings.ModeBranch])
            .Concat(Global.Settings.ModeRepositoryPath.Split('/')).Concat(path.Replace('\\', '/').Split('/')).Select(Uri.EscapeDataString));

    private static string RulesTreeApiUrl()
    {
        var branch = Uri.EscapeDataString(Global.Settings.ModeBranch);
        return $"https://api.github.com/repos/{Global.Settings.ModeRepository}/git/trees/{branch}?recursive=1";
    }

    private static async Task<bool> CheckRulesAsync()
    {
        try
        {
            var apiResult = await FetchApiCachedAsync(RulesTreeApiUrl());
            if (apiResult.Body != null)
            {
                ApplyRulesTreeJson(apiResult.Body);
                lock (CacheLock)
                {
                    _cache.RulesSourceKey = SourceKey;
                    _cache.RulesTreeSha = RemoteTree;
                    _cache.RemoteRuleCount = RemoteRuleCount;
                }
            }
            if (apiResult.RateLimitError != null)
                Errors["Rules"] = apiResult.RateLimitError;
            return apiResult.Body != null;
        }
        catch (Exception e)
        {
            Errors["Rules"] = e.Message;
            return false;
        }
    }

    private static void ApplyRulesTreeJson(string json)
    {
        using var response = JsonDocument.Parse(json);
        if (response.RootElement.GetProperty("truncated").GetBoolean())
            throw new InvalidDataException("The GitHub tree response is incomplete.");
        var prefix = Global.Settings.ModeRepositoryPath.Trim('/') + "/";
        var tree = response.RootElement.GetProperty("tree");
        var files = tree.EnumerateArray()
            .Where(item => item.GetProperty("type").GetString() == "blob")
            .Select(item => (Path: item.GetProperty("path").GetString()!, Sha: item.GetProperty("sha").GetString()!))
            .Where(item => item.Path.StartsWith(prefix, StringComparison.Ordinal))
            .Select(item => (Path: item.Path[prefix.Length..].Replace('/', Path.DirectorySeparatorChar), item.Sha))
            .Where(item => Path.GetExtension(item.Path).ToLowerInvariant() is ".txt" or ".json" && !Path.IsPathRooted(item.Path)
                && item.Path.Split(Path.DirectorySeparatorChar).All(part => part is not ("." or "..") && part.IndexOfAny(Path.GetInvalidFileNameChars()) < 0))
            .ToArray();
        if (files.Length == 0)
            throw new InvalidDataException("No mode files were found in the configured source.");
        RemoteTree = response.RootElement.GetProperty("sha").GetString() ?? "";
        RemoteRuleCount = files.Length;
        var next = new List<RuleChange>();
        if (_manifest.Source != SourceKey)
            _manifest = new RuleManifest { Source = SourceKey };
        foreach (var (path, sha) in files)
        {
            var (raw, normalized) = LocalBlobs(path);
            if (raw == sha || normalized == sha)
            {
                _manifest.Blobs[path] = sha;
                continue;
            }

            _manifest.Blobs.TryGetValue(path, out var baseline);
            var local = baseline != null && baseline == normalized ? normalized : raw;
            var kind = Classify(local, sha, baseline);
            if (kind.HasValue)
                next.Add(new RuleChange(path, sha, raw, kind.Value));
        }
        Rules = next;
        WriteJson(BaseRulesPath, _manifest);
    }

    internal static RuleChangeKind? Classify(string? local, string remote, string? baseline)
    {
        if (local == null) return RuleChangeKind.Added;
        if (local == remote || remote == baseline) return null;
        return local == baseline ? RuleChangeKind.Updated : RuleChangeKind.Conflict;
    }

    public static ModeOrigin Origin(string path)
    {
        if (Rules.FirstOrDefault(change => change.Path.Equals(path, StringComparison.OrdinalIgnoreCase)) is { } rule)
            return rule.Kind == RuleChangeKind.Conflict ? ModeOrigin.Modified : ModeOrigin.Update;
        if (_manifest.Blobs.TryGetValue(path, out var baseline))
        {
            var (raw, normalized) = LocalBlobs(path);
            return baseline == raw || baseline == normalized ? ModeOrigin.Bundled : ModeOrigin.Modified;
        }
        return ModeOrigin.Custom;
    }

    private static string? LocalBlob(string path)
    {
        var fullPath = ModeService.Instance.GetFullPath(path);
        return File.Exists(fullPath) ? BlobId(File.ReadAllBytes(fullPath)) : null;
    }

    private static (string? Raw, string? Normalized) LocalBlobs(string path)
    {
        var fullPath = ModeService.Instance.GetFullPath(path);
        if (!File.Exists(fullPath))
            return (null, null);
        var bytes = File.ReadAllBytes(fullPath);
        var raw = BlobId(bytes);
        // Windows checkout can expand LF to CRLF while GitHub keeps the original text blob.
        if (!bytes.AsSpan().Contains((byte)'\r'))
            return (raw, raw);
        var lf = Encoding.Latin1.GetString(bytes).Replace("\r\n", "\n", StringComparison.Ordinal);
        return (raw, BlobId(Encoding.Latin1.GetBytes(lf)));
    }

    internal static string BlobId(byte[] bytes)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA1);
        hash.AppendData(Encoding.ASCII.GetBytes($"blob {bytes.Length}\0"));
        hash.AppendData(bytes);
        return Convert.ToHexStringLower(hash.GetHashAndReset());
    }

    public static void SourceChanged()
    {
        EnsureCacheLoaded();
        Rules = [];
        RemoteRuleCount = 0;
        RemoteTree = "";
        lock (CacheLock)
        {
            _cache.RulesSourceKey = null;
            _cache.RulesTreeSha = null;
            _cache.RemoteRuleCount = 0;
        }
        SaveCache();
        Changed?.Invoke();
    }

    public static async Task<(string[] Added, string[] Removed)> DiffAsync(RuleChange change)
    {
        using var client = CreateClient();
        var remote = (await client.GetStringAsync(Request(RuleUrl(change.Path)))).GetLines().ToHashSet(StringComparer.Ordinal);
        var path = ModeService.Instance.GetFullPath(change.Path);
        var local = File.Exists(path) ? (await File.ReadAllLinesAsync(path)).Where(l => l.Length > 0).ToHashSet(StringComparer.Ordinal) : [];
        return (remote.Except(local).ToArray(), local.Except(remote).ToArray());
    }

    public static async Task SyncAsync(IReadOnlyList<(RuleChange Rule, ConflictAction Action)> selection, Action<string> onApplied)
    {
        if (Applying || Checking || selection.Count == 0)
            return;
        Applying = true;
        Changed?.Invoke();
        var written = 0;
        try
        {
            using var client = CreateClient();
            var ready = new List<(RuleChange Rule, ConflictAction Action, byte[] Content)>();
            foreach (var (rule, action) in selection.Where(item => item.Action != ConflictAction.Keep))
            {
                var bytes = await client.GetByteArrayAsync(Request(RuleUrl(rule.Path)));
                if (BlobId(bytes) != rule.Sha)
                    throw new InvalidDataException($"The remote rule changed. Check for updates again: {rule.Path}");
                ready.Add((rule, action, bytes));
            }

            if (ready.Count == 0)
                return;

            foreach (var (rule, action, _) in ready)
                if (action == ConflictAction.Replace && LocalBlob(rule.Path) != rule.LocalSha)
                    throw new InvalidDataException($"The local rule changed. Check for updates again: {rule.Path}");

            try
            {
                foreach (var (rule, action, content) in ready)
                {
                    var path = ModeService.Instance.GetFullPath(rule.Path);
                    if (action == ConflictAction.Copy)
                    {
                        var filename = Path.GetFileNameWithoutExtension(path);
                        var extension = Path.GetExtension(path);
                        var directory = Path.GetDirectoryName(path)!;
                        path = Path.Combine(directory, filename + " (remote)" + extension);
                        for (var n = 2; File.Exists(path); n++)
                            path = Path.Combine(directory, $"{filename} (remote {n}){extension}");
                    }
                    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                    WriteBytes(path, content);
                    written++;
                    if (action == ConflictAction.Replace)
                    {
                        _manifest.Blobs[rule.Path] = rule.Sha;
                        Rules.Remove(rule);
                    }
                    onApplied(rule.Path);
                }
            }
            finally
            {
                if (written > 0)
                {
                    _manifest.SynchronizedAt = DateTimeOffset.UtcNow;
                    try { WriteJson(BaseRulesPath, _manifest); }
                    finally { ModeService.Instance.Load(completeSceneDefaults: true); }
                }
            }
            Record("Mode rules", $"{written}", true);
        }
        catch (Exception e)
        {
            Record("Mode rules", $"{written}", false, e.Message);
            throw;
        }
        finally
        {
            Applying = false;
            Changed?.Invoke();
        }
    }

    public static bool InUse(CorePackage core) => Session.Current.IsRunning && (core.Name switch
    {
        "Xray-core" => MainController.ServerController?.Name == "Xray",
        "WinTUN" => MainController.ModeControllers.Any(c => c is TUNController) || MainController.ModeController is TUNController,
        _ => core.Name.Equals(MainController.ServerController?.Name, StringComparison.OrdinalIgnoreCase) ||
             MainController.ModeControllers.Any(c => core.Name.Equals(c.Name, StringComparison.OrdinalIgnoreCase)) ||
             core.Name.Equals(MainController.ModeController?.Name, StringComparison.OrdinalIgnoreCase)
    });

    public static string CorePath(CorePackage core) => Path.Combine(Bootstrap.BinDirectory, core.File);

    private static string[] CoreFiles(CorePackage core)
    {
        var path = CorePath(core);
        return core.Name == "sing-box" ? [path, Path.Combine(Path.GetDirectoryName(path)!, "libcronet.dll")] : [path];
    }

    private static string PreviousPath(CorePackage core) => CorePath(core) +
        (core.Name == "sing-box" ? ".previous.zip" : ".previous");

    public static bool HasRollback(CorePackage core) => File.Exists(PreviousPath(core));

    public static Task<bool> InstallAsync(CorePackage core, UpdateChoice version) =>
        RunTransferAsync(core, transfer => InstallCoreAsync(core, version, transfer));

    public static Task<bool> UpdateApplicationAsync() =>
        RunTransferAsync(null, async transfer => await AppUpdate.RestartAsync(await DownloadApplicationAsync(transfer)));

    public static Task<bool> UpdatePacRuleAsync(PacRulePreset preset, string? customUrl = null, bool reloadActiveScene = true) =>
        RunTransferAsync(null, transfer => DownloadPacRuleAsync(preset, customUrl, transfer, reloadActiveScene), preset);

    public static void ReloadActivePacSceneIfNeeded(IReadOnlyCollection<PacRulePreset> updatedPresets)
    {
        if (updatedPresets.Count == 0 || App.IsExiting)
            return;

        if (Session.Current.IsRunning && Session.Current.ActiveScene is { } activeScene &&
            MainController.Modes.OfType<WebProxyMode>().FirstOrDefault() is { Strategy: WebProxyStrategy.Pac } activeWeb &&
            updatedPresets.Contains(activeWeb.PacPreset))
        {
            if (App.MainWindow is { } window)
                window.DispatcherQueue.TryEnqueue(() => Session.Current.StartAsync(activeScene).Forget());
            else
                Session.Current.StartAsync(activeScene).Forget();
        }
    }

    private static async Task<bool> RunTransferAsync(CorePackage? core, Func<DownloadTransfer, Task> download, PacRulePreset? pacPreset = null)
    {
        if (ActiveTransfer != null || Applying || Checking)
            return false;

        var transfer = new DownloadTransfer(core, pacPreset);
        ActiveTransfer = transfer;
        Applying = true;
        try
        {
            Changed?.Invoke();
            await download(transfer);
            return true;
        }
        finally
        {
            ActiveTransfer = null;
            Applying = false;
            Changed?.Invoke();
        }
    }

    private static void ReportProgress(DownloadTransfer transfer, TransferProgress progress)
    {
        transfer.Progress = progress;
        ProgressChanged?.Invoke();
    }

    private static async Task DownloadPacRuleAsync(PacRulePreset preset, string? customUrl, DownloadTransfer transfer, bool reloadActiveScene)
    {
        var file = PacRuleService.GetFileName(preset)
            ?? throw new InvalidOperationException("The selected PAC preset does not use a remote rule list.");

        var candidates = new List<(string Url, string? ReleaseTag)>();
        if (!string.IsNullOrWhiteSpace(customUrl))
        {
            candidates.Add((customUrl.Trim(), null));
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(RemotePacVersion))
                candidates.Add((Request($"{PacRuleService.ReleasesUrl}/download/{RemotePacVersion}/{file}"), RemotePacVersion));

            if (PacRuleService.GetDefaultUrl(preset) is { } defaultUrl)
                candidates.Add((Request(defaultUrl), RemotePacVersion));

            if (Global.Settings.DownloadChannel == DownloadChannel.Direct && PacRuleService.GetFallbackUrl(preset) is { } cdnUrl)
                candidates.Add((cdnUrl, RemotePacVersion));
        }

        try
        {
            using var client = CreateClient();
            client.Timeout = TimeSpan.FromMilliseconds(Math.Max(Global.Settings.RequestTimeout, 20000));

            string? content = null;
            string? usedTag = null;
            Exception? lastError = null;
            for (var attempt = 0; attempt < candidates.Count; attempt++)
            {
                var (url, releaseTag) = candidates[attempt];
                if (attempt > 0)
                    ReportProgress(transfer, new TransferProgress(-1, 0));

                try
                {
                    using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                    if (response.StatusCode != HttpStatusCode.OK)
                        throw new HttpRequestException($"HTTP {(int)response.StatusCode} ({response.ReasonPhrase})", null, response.StatusCode);

                    using var buffer = new MemoryStream();
                    await using (var source = await response.Content.ReadAsStreamAsync())
                    {
                        await TransferAsync(source, buffer, response.Content.Headers.ContentLength, transfer);
                    }

                    buffer.Position = 0;
                    using var reader = new StreamReader(buffer, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                    content = await reader.ReadToEndAsync();
                    usedTag = releaseTag;
                    break;
                }
                catch (Exception ex) when (ex is HttpRequestException or IOException or TaskCanceledException)
                {
                    lastError = ex;
                    Log.Warning(ex, "PAC download attempt failed for {Url}", url);
                }
            }

            if (content == null)
                throw lastError ?? new HttpRequestException($"Failed to download PAC rule list {file}.");

            var (_, ruleCount) = await PacRuleService.SaveValidatedContentAsync(preset, content);

            if (!string.IsNullOrWhiteSpace(usedTag))
                _pacManifest.Versions[file] = usedTag;
            else
                _pacManifest.Versions.Remove(file);
            _pacManifest.SynchronizedAt = DateTimeOffset.UtcNow;
            Directory.CreateDirectory(Path.GetDirectoryName(PacManifestPath)!);
            WriteJson(PacManifestPath, _pacManifest);
            Errors.TryRemove("PAC", out _);

            ReportProgress(transfer, new TransferProgress(100, 0));
            var versionLabel = !string.IsNullOrWhiteSpace(customUrl) ? "custom" : usedTag ?? PacRuleService.Branch;
            Record($"PAC · {file}", versionLabel, true, $"{ruleCount:N0} rules");

            if (reloadActiveScene)
                ReloadActivePacSceneIfNeeded([preset]);
        }
        catch (Exception e)
        {
            var failedVersion = !string.IsNullOrWhiteSpace(customUrl) ? "custom" : RemotePacVersion ?? PacRuleService.Branch;
            Record($"PAC · {file}", failedVersion, false, e.Message);
            throw;
        }
    }

    private static async Task InstallCoreAsync(CorePackage core, UpdateChoice version, DownloadTransfer transfer)
    {
        try
        {
            using var client = CreateClient();
            using var response = await client.GetAsync(Request(version.Url), HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            using var archiveFile = File.Create(Path.GetTempFileName(), 81920, FileOptions.DeleteOnClose);
            await using (var source = await response.Content.ReadAsStreamAsync())
            {
                await TransferAsync(source, archiveFile, response.Content.Headers.ContentLength, transfer);
            }
            archiveFile.Position = 0;
            using var zip = new ZipArchive(archiveFile, ZipArchiveMode.Read);
            var fileName = core.ArchiveFile ?? Path.GetFileName(core.File);
            var entry = zip.Entries.SingleOrDefault(e => core.ArchiveFile?.Contains('/') == true ? e.FullName == fileName : e.Name == fileName)
                ?? throw new InvalidDataException($"The archive is missing {fileName}.");
            var paths = CoreFiles(core);
            Directory.CreateDirectory(Path.GetDirectoryName(paths[0])!);
            try
            {
                entry.ExtractToFile(paths[0] + ".download", overwrite: true);
                if (paths.Length > 1)
                {
                    var dllName = entry.FullName[..(entry.FullName.LastIndexOf('/') + 1)] + Path.GetFileName(paths[1]);
                    var dll = zip.GetEntry(dllName) ?? throw new InvalidDataException($"The archive is missing {dllName}.");
                    dll.ExtractToFile(paths[1] + ".download", overwrite: true);
                }
                await Session.Current.RunCoreReplacementAsync(() => ReplaceCoreAsync(core, paths, version.Version));
            }
            finally
            {
                foreach (var path in paths)
                    File.Delete(path + ".download");
            }

            ReportProgress(transfer, new TransferProgress(100, 0));
            Record(core.Name, version.Version, true);
        }
        catch (Exception e)
        {
            Record(core.Name, version.Version, false, e.Message);
            throw;
        }
    }

    private static async Task ReplaceCoreAsync(CorePackage core, string[] paths, string? expectedVersion = null)
    {
        var session = Session.Current;
        var scene = InUse(core) ? session.ActiveScene : null;
        var previous = PreviousPath(core);
        var hadFiles = paths.Select(File.Exists).ToArray();
        if (scene != null)
            await session.StopCoreAsync();

        var replaced = 0;
        try
        {
            for (; replaced < paths.Length; replaced++)
            {
                var path = paths[replaced];
                if (hadFiles[replaced])
                    File.Replace(path + ".download", path, path + ".download-backup");
                else
                    File.Move(path + ".download", path);
            }

            var installed = await ReadVersionAsync(core);
            if (installed.Length == 0)
                throw new InvalidDataException(Helpers.Ui.T("The replacement core could not report its version.",
                    "替换后的核心无法报告版本。"));
            if (expectedVersion != null && !string.Equals(installed, expectedVersion, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"Expected {expectedVersion}, but the replacement reports {installed}.");
            Installed[core] = installed;
            if (scene != null && !App.IsExiting)
            {
                await session.StartCoreAsync(scene);
                if (!session.IsRunning || session.ActiveScene != scene)
                    throw new InvalidDataException(Helpers.Ui.T("The updated core could not reconnect the scene.",
                        "更新后的核心未能重新连接场景。") + " " + session.ErrorMessage);
            }
            if (hadFiles.All(present => present))
            {
                if (paths.Length == 1)
                    File.Move(paths[0] + ".download-backup", previous, overwrite: true);
                else
                    SavePreviousSet(paths, previous);
            }
        }
        catch
        {
            if (replaced > 0)
            {
                if (scene != null && session.IsRunning && session.ActiveScene == scene)
                    await session.StopCoreAsync();
                while (replaced > 0)
                {
                    var index = --replaced;
                    var path = paths[index];
                    if (hadFiles[index])
                        File.Replace(path + ".download-backup", path, null);
                    else
                        File.Delete(path);
                }
                Installed[core] = await ReadVersionAsync(core);
            }
            if (scene != null && !App.IsExiting)
                await session.StartCoreAsync(scene);
            throw;
        }

        foreach (var path in paths)
        {
            try { File.Delete(path + ".download-backup"); }
            catch (IOException e) { Log.Warning(e, "Remove temporary core backup {Path}", path); }
            catch (UnauthorizedAccessException e) { Log.Warning(e, "Remove temporary core backup {Path}", path); }
        }
    }

    private static void SavePreviousSet(string[] paths, string previous)
    {
        var temporary = previous + ".tmp";
        try
        {
            using (var stream = File.Create(temporary))
            using (var zip = new ZipArchive(stream, ZipArchiveMode.Create))
                foreach (var path in paths)
                    zip.CreateEntryFromFile(path + ".download-backup", Path.GetFileName(path), CompressionLevel.NoCompression);
            File.Move(temporary, previous, overwrite: true);
        }
        finally { File.Delete(temporary); }
    }

    public static async Task RollbackAsync(CorePackage core)
    {
        if (Applying || Checking)
            return;
        Applying = true;
        var paths = CoreFiles(core);
        var previous = PreviousPath(core);
        try
        {
            Changed?.Invoke();
            if (paths.Length == 1)
                File.Copy(previous, paths[0] + ".download", overwrite: true);
            else
            {
                using var zip = ZipFile.OpenRead(previous);
                foreach (var path in paths)
                {
                    var entry = zip.GetEntry(Path.GetFileName(path))
                        ?? throw new InvalidDataException($"The backup is missing {Path.GetFileName(path)}.");
                    entry.ExtractToFile(path + ".download", overwrite: true);
                }
            }
            await Session.Current.RunCoreReplacementAsync(() => ReplaceCoreAsync(core, paths));
            Record(core.Name, Installed[core], true, "Rollback");
        }
        finally
        {
            Applying = false;
            foreach (var path in paths)
                File.Delete(path + ".download");
            Changed?.Invoke();
        }
    }

    private static async Task<string> DownloadApplicationAsync(DownloadTransfer transfer)
    {
        var asset = AppAsset ?? throw new InvalidOperationException("This release has no WinUI x64 ZIP.");
        var file = Path.Combine(Path.GetTempPath(), $"Netch-update-{Guid.NewGuid():N}.zip");
        using var client = CreateClient();
        using var response = await client.GetAsync(Request(asset.browser_download_url), HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        try
        {
            await using var target = File.Create(file);
            await using var source = await response.Content.ReadAsStreamAsync();
            await TransferAsync(source, target, response.Content.Headers.ContentLength, transfer);
            Record(UpdateChecker.Name, AppRelease!.tag_name, true);
            return file;
        }
        catch
        {
            File.Delete(file);
            throw;
        }
    }

    private static async Task TransferAsync(Stream source, Stream target, long? total, DownloadTransfer transfer)
    {
        var buffer = new byte[81920];
        var reported = int.MinValue;
        ulong speed = 0;
        var transferred = 0L;
        var sample = Stopwatch.StartNew();
        while (await source.ReadAsync(buffer) is var count && count > 0)
        {
            sample.Stop();
            await transfer.WaitWhilePausedAsync();
            sample.Start();
            await target.WriteAsync(buffer.AsMemory(0, count));
            transferred += count;
            var percent = total > 0 ? (int)(target.Length * 100.0 / total.Value) : -1;
            var elapsed = sample.Elapsed.TotalSeconds;
            var sampled = elapsed >= 0.25;
            if (sampled)
            {
                speed = (ulong)(transferred / elapsed);
                transferred = 0;
                sample.Restart();
            }
            if (sampled || percent != reported)
            {
                reported = percent;
                ReportProgress(transfer, new TransferProgress(percent, speed));
            }
        }
        while (transfer.IsPaused)
            await transfer.WaitWhilePausedAsync();
        transfer.FinishReceiving();
        Changed?.Invoke();
    }

    private static void Record(string name, string version, bool ok, string? message = null)
    {
        History.Insert(0, new DownloadEntry(DateTime.Now, name, version, ok, message));
        if (History.Count > 100) History.RemoveAt(History.Count - 1);
        WriteJson(HistoryPath, History);
    }

    private static T? ReadJson<T>(string path)
    {
        try { return File.Exists(path) ? JsonSerializer.Deserialize<T>(File.ReadAllText(path)) : default; }
        catch (JsonException e) { Log.Warning(e, "Read {Path}", path); return default; }
    }

    private static void WriteJson<T>(string path, T value) =>
        WriteBytes(path, JsonSerializer.SerializeToUtf8Bytes(value, new JsonSerializerOptions { WriteIndented = true }));

    private static void WriteBytes(string path, byte[] content)
    {
        var temporary = path + ".tmp";
        try
        {
            File.WriteAllBytes(temporary, content);
            File.Move(temporary, path, overwrite: true);
        }
        finally { File.Delete(temporary); }
    }
}
