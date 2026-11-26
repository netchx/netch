using System.Globalization;
using System.Net;
using Netch.Controllers;
using Netch.Enums;
using Netch.Models;
using Netch.Models.Modes.WebProxyMode;
using Netch.Utils;

namespace Netch.Services;

public sealed record PacRuleStatus(
    bool Exists,
    string? FilePath,
    bool IsUserUpdated,
    DateTime? LastWriteTime,
    int RuleCount);

public static class PacRuleService
{
    public const string Repository = "Loyalsoldier/v2ray-rules-dat";
    public const string Branch = "release";
    public const string ReleasesUrl = "https://github.com/Loyalsoldier/v2ray-rules-dat/releases";
    public static readonly PacRulePreset[] RemotePresets = [PacRulePreset.ProxyList, PacRulePreset.GfwList, PacRulePreset.BypassChina];

    private const string RawBaseUrl = "https://raw.githubusercontent.com/Loyalsoldier/v2ray-rules-dat/release/";
    private const string JsDelivrBaseUrl = "https://cdn.jsdelivr.net/gh/Loyalsoldier/v2ray-rules-dat@release/";

    public static string? GetFileName(PacRulePreset preset) => preset switch
    {
        PacRulePreset.ProxyList => "proxy-list.txt",
        PacRulePreset.GfwList => "gfw.txt",
        PacRulePreset.BypassChina => "direct-list.txt",
        _ => null
    };

    public static string? GetDefaultUrl(PacRulePreset preset) =>
        GetFileName(preset) is { } file ? RawBaseUrl + file : null;

    public static string? GetFallbackUrl(PacRulePreset preset) =>
        GetFileName(preset) is { } file ? JsDelivrBaseUrl + file : null;

    public static string GetUserRuleDirectory(string? baseDir = null)
    {
        var root = string.IsNullOrWhiteSpace(baseDir) ? Global.NetchDir : baseDir;
        return Path.Combine(root, "data", "pac");
    }

    public static string GetBundledRuleDirectory(string? baseDir = null)
    {
        var root = string.IsNullOrWhiteSpace(baseDir) ? Global.NetchDir : baseDir;
        return Path.Combine(root, "bin", "pac");
    }

    public static string? ResolveRuleFilePath(PacRulePreset preset, string? baseDir = null)
    {
        if (GetFileName(preset) is not { } file)
            return null;

        var userPath = Path.Combine(GetUserRuleDirectory(baseDir), file);
        if (File.Exists(userPath))
            return userPath;

        var bundledPath = Path.Combine(GetBundledRuleDirectory(baseDir), file);
        return File.Exists(bundledPath) ? bundledPath : null;
    }

    public static PacRuleStatus GetRuleStatus(PacRulePreset preset, string? baseDir = null, bool includeRuleCount = true)
    {
        if (GetFileName(preset) is not { } file)
            return new PacRuleStatus(false, null, false, null, 0);

        var userPath = Path.Combine(GetUserRuleDirectory(baseDir), file);
        var path = ResolveRuleFilePath(preset, baseDir);
        if (path == null)
            return new PacRuleStatus(false, null, false, null, 0);

        var isUser = string.Equals(path, userPath, StringComparison.OrdinalIgnoreCase);
        DateTime? lastWrite = null;
        var count = 0;
        try
        {
            lastWrite = File.GetLastWriteTime(path);
            if (includeRuleCount)
                count = CountRules(path);
        }
        catch (IOException)
        {
            // Return best-effort metadata if file was modified concurrently.
        }

        return new PacRuleStatus(true, path, isUser, lastWrite, count);
    }

    private static int CountRules(string path)
    {
        var count = 0;
        var seenAny = false;
        var maybeBase64 = true;
        foreach (var line in File.ReadLines(path))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#') || trimmed.StartsWith("//") ||
                trimmed.StartsWith('!') || trimmed.StartsWith('['))
                continue;

            seenAny = true;
            if (trimmed.Contains('.') || trimmed.Contains(':'))
                maybeBase64 = false;

            count++;
        }

        if (seenAny && maybeBase64)
        {
            var parsed = PacScriptBuilder.ParseRuleText(File.ReadAllText(path));
            return parsed.SuffixDomains.Count + parsed.ExactDomains.Count + parsed.RegexPatterns.Count +
                   parsed.BypassSuffixDomains.Count + parsed.BypassExactDomains.Count;
        }

        return count;
    }

    public static async Task<(string SavedPath, int RuleCount)> DownloadAndSaveAsync(
        PacRulePreset preset,
        string? customUrl = null,
        string? proxyServer = null,
        string? githubMirror = null,
        string? baseDir = null,
        CancellationToken cancellationToken = default)
    {
        var file = GetFileName(preset)
            ?? throw new InvalidOperationException("The selected PAC preset does not use a remote rule list.");

        var primaryUrl = ResolveDownloadUrl(preset, customUrl, proxyServer, githubMirror);
        var fallbackUrl = string.IsNullOrWhiteSpace(customUrl) && string.IsNullOrWhiteSpace(githubMirror) && string.IsNullOrWhiteSpace(proxyServer)
            ? GetFallbackUrl(preset)
            : null;

        string content;
        using var client = WebUtil.CreateHttpClient(timeout: Math.Max(Global.Settings.RequestTimeout, 20000), proxyServer: proxyServer);
        try
        {
            content = await DownloadStringAsync(client, primaryUrl, cancellationToken);
        }
        catch (Exception ex) when (fallbackUrl != null && ex is HttpRequestException or IOException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            Log.Warning(ex, "Primary PAC rule download failed for {Url}, retrying via CDN {Fallback}", primaryUrl, fallbackUrl);
            content = await DownloadStringAsync(client, fallbackUrl, cancellationToken);
        }

        return await SaveValidatedContentAsync(preset, content, baseDir, cancellationToken);
    }

    public static async Task<(string SavedPath, int RuleCount)> SaveValidatedContentAsync(
        PacRulePreset preset,
        string content,
        string? baseDir = null,
        CancellationToken cancellationToken = default)
    {
        var file = GetFileName(preset)
            ?? throw new InvalidOperationException("The selected PAC preset does not use a remote rule list.");

        var parsed = PacScriptBuilder.ParseRuleText(content);
        var ruleCount = parsed.SuffixDomains.Count + parsed.ExactDomains.Count + parsed.RegexPatterns.Count +
                        parsed.BypassSuffixDomains.Count + parsed.BypassExactDomains.Count;
        if (ruleCount == 0)
            throw new InvalidDataException(i18N.Translate("The downloaded PAC rule file contains no valid rules."));

        var dir = GetUserRuleDirectory(baseDir);
        Directory.CreateDirectory(dir);
        var targetPath = Path.Combine(dir, file);
        var tempPath = targetPath + ".tmp";
        try
        {
            await File.WriteAllTextAsync(tempPath, content, cancellationToken);
            File.Move(tempPath, targetPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }

        Log.Information("Updated PAC rule list {File} ({Count} rules)", file, ruleCount);
        return (targetPath, ruleCount);
    }

    public static bool TryParseReleaseTagTimestampUtc(string? tag, out DateTimeOffset timestamp)
    {
        timestamp = default;
        if (string.IsNullOrWhiteSpace(tag))
            return false;

        return DateTimeOffset.TryParseExact(
            tag.Trim(),
            "yyyyMMddHHmm",
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out timestamp);
    }

    public static bool IsUpdateAvailable(PacRuleStatus status, string? installedVersion, string? remoteVersion)
    {
        ArgumentNullException.ThrowIfNull(status);
        if (!status.Exists)
            return true;

        if (string.IsNullOrWhiteSpace(remoteVersion))
            return false;

        if (!string.IsNullOrWhiteSpace(installedVersion))
            return !string.Equals(installedVersion.Trim(), remoteVersion.Trim(), StringComparison.OrdinalIgnoreCase);

        if (status.LastWriteTime is { } localWrite && TryParseReleaseTagTimestampUtc(remoteVersion, out var remoteUtc))
            return remoteUtc > localWrite.ToUniversalTime().AddHours(1);

        return false;
    }

    public static async Task<IReadOnlyDictionary<PacRulePreset, int>> DownloadAllPresetsAsync(
        string? proxyServer = null,
        string? githubMirror = null,
        string? baseDir = null,
        CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<PacRulePreset, int>();
        foreach (var preset in RemotePresets)
        {
            var (_, count) = await DownloadAndSaveAsync(preset, null, proxyServer, githubMirror, baseDir, cancellationToken);
            results[preset] = count;
        }
        return results;
    }

    public static (string? ProxyServer, string? GitHubMirror) GetCurrentDownloadOptions()
    {
        return Global.Settings.DownloadChannel switch
        {
            DownloadChannel.Proxy => (
                MainController.Socks5Server is { } active
                    ? WebUtil.BuildSocks5ProxyUri(active)
                    : throw new MessageException(i18N.Translate("Connect a scene to use the current proxy.")),
                null),
            DownloadChannel.GitHubMirror => (null, Global.Settings.GitHubMirror),
            _ => (null, null)
        };
    }

    public static string ResolveDownloadUrl(PacRulePreset preset, string? customUrl, string? proxyServer, string? githubMirror)
    {
        var rawUrl = !string.IsNullOrWhiteSpace(customUrl)
            ? customUrl.Trim()
            : GetDefaultUrl(preset) ?? throw new InvalidOperationException("No default URL for this PAC preset.");

        if (!string.IsNullOrWhiteSpace(customUrl) || !string.IsNullOrWhiteSpace(proxyServer) || string.IsNullOrWhiteSpace(githubMirror))
            return rawUrl;

        if (Uri.TryCreate(rawUrl, UriKind.Absolute, out var uri) &&
            uri.Host is "github.com" or "api.github.com" or "raw.githubusercontent.com")
        {
            return $"{githubMirror.Trim().TrimEnd('/')}/{rawUrl}";
        }

        return rawUrl;
    }

    private static async Task<string> DownloadStringAsync(HttpClient client, string url, CancellationToken cancellationToken)
    {
        using var response = await client.GetAsync(url, HttpCompletionOption.ResponseContentRead, cancellationToken);
        if (response.StatusCode != HttpStatusCode.OK)
            throw new HttpRequestException($"HTTP {(int)response.StatusCode} ({response.ReasonPhrase})", null, response.StatusCode);

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }
}
