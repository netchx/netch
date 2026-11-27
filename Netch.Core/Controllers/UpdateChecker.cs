using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Netch.Models.GitHubRelease;
using Netch.Services;
using Netch.Utils;

namespace Netch.Controllers;

public static class UpdateChecker
{
    public const string Owner = @"NetchX";
    public const string Repo = @"Netch";
    public const string ProjectUrl = @"https://github.com/NetchX/Netch";

    public const string Name = @"Netch";
    public const string Copyright = @"Copyright © 2019 - 2026";
    public const string SelfContainedDeploymentMode = "self-contained";
    public const string FrameworkDependentDeploymentMode = "framework-dependent";

    public static readonly string Version = ResolveVersion();
    public static readonly bool IsSelfContained = ResolveIsSelfContained();

    internal static bool ResolveIsSelfContained(Assembly? entryAssembly = null)
    {
        var assembly = entryAssembly ?? Assembly.GetEntryAssembly();
        var mode = assembly?.GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attr => string.Equals(attr.Key, "DeploymentMode", StringComparison.OrdinalIgnoreCase))
            ?.Value;
        return !string.Equals(mode?.Trim(), FrameworkDependentDeploymentMode, StringComparison.OrdinalIgnoreCase);
    }

    public static Asset? SelectReleaseAsset(IEnumerable<Asset>? assets, bool selfContained)
    {
        if (assets == null)
            return null;

        var candidates = assets
            .Where(asset => asset != null
                && UpdateArchiveExtractor.IsSupportedArchiveName(asset.name)
                && asset.name.Contains("winui", StringComparison.OrdinalIgnoreCase)
                && asset.name.Contains("x64", StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (candidates.Count == 0)
            return null;

        if (selfContained)
        {
            return SelectPreferredArchive(candidates.Where(asset => asset.name.Contains(SelfContainedDeploymentMode, StringComparison.OrdinalIgnoreCase)))
                ?? SelectPreferredArchive(candidates.Where(asset => !asset.name.Contains(FrameworkDependentDeploymentMode, StringComparison.OrdinalIgnoreCase)));
        }

        return SelectPreferredArchive(candidates.Where(asset => asset.name.Contains(FrameworkDependentDeploymentMode, StringComparison.OrdinalIgnoreCase)))
            ?? SelectPreferredArchive(candidates.Where(asset => asset.name.Contains(SelfContainedDeploymentMode, StringComparison.OrdinalIgnoreCase)))
            ?? SelectPreferredArchive(candidates);
    }

    private static Asset? SelectPreferredArchive(IEnumerable<Asset> candidates)
    {
        Asset? zipFallback = null;
        foreach (var candidate in candidates)
        {
            if (candidate.name.EndsWith(".7z", StringComparison.OrdinalIgnoreCase))
                return candidate;

            zipFallback ??= candidate;
        }

        return zipFallback;
    }

    internal static string ResolveVersion(string? informationalVersion = null, Version? assemblyVersion = null)
    {
        var raw = informationalVersion
            ?? typeof(UpdateChecker).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(raw))
        {
            var plus = raw.IndexOf('+');
            if (plus >= 0)
                raw = raw[..plus];

            raw = raw.Trim().TrimStart('v', 'V');
            if (raw.Length > 0)
                return raw;
        }

        var ver = assemblyVersion ?? typeof(UpdateChecker).Assembly.GetName().Version;
        if (ver != null)
            return ver.Revision > 0
                ? $"{ver.Major}.{ver.Minor}.{ver.Build}.{ver.Revision}"
                : $"{ver.Major}.{ver.Minor}.{Math.Max(0, ver.Build)}";

        return "0.0.0";
    }

    public static Release LatestRelease = new();

    public static string LatestVersionNumber =>
        SuffixVersion.TryParse(LatestRelease.tag_name, out var version)
            ? version.ToString()
            : LatestRelease.tag_name;

    public static string DisplayVersion =>
        string.IsNullOrWhiteSpace(LatestRelease.tag_name) ? Version : LatestRelease.tag_name;

    public static string LatestVersionUrl => LatestRelease.html_url;

    public static event EventHandler? NewVersionFound;

    public static event EventHandler? NewVersionFoundFailed;

    public static event EventHandler? NewVersionNotFound;

    public static async Task CheckAsync(bool isPreRelease)
    {
        try
        {
            var updater = new GitHubRelease(Owner, Repo);
            var url = updater.AllReleaseUrl;

            var (_, json) = await WebUtil.DownloadStringAsync(url);

            var releases = JsonSerializer.Deserialize<List<Release>>(json);
            ArgumentNullException.ThrowIfNull(releases);
            LatestRelease = GetLatestRelease(releases, isPreRelease);
            Log.Information("Github latest release: {Version}", LatestRelease.tag_name);
            if (VersionUtil.CompareVersion(LatestRelease.tag_name, Version) > 0)
            {
                Log.Information("Found newer version");
                NewVersionFound?.Invoke(null, EventArgs.Empty);
            }
            else
            {
                Log.Information("Already the latest version");
                NewVersionNotFound?.Invoke(null, EventArgs.Empty);
            }
        }
        catch (Exception e)
        {
            if (e is WebException)
                Log.Warning(e, "Get releases failed");
            else
                Log.Error(e, "Get releases error");

            NewVersionFoundFailed?.Invoke(null, EventArgs.Empty);
        }
    }

    public static (string fileName, string sha256) GetLatestUpdateFileNameAndHash(string? keyword = null)
    {
        var body = LatestRelease.body ?? string.Empty;
        var hashSectionIndex = body.IndexOf("## SHA256", StringComparison.OrdinalIgnoreCase);
        if (hashSectionIndex < 0)
            hashSectionIndex = body.IndexOf("校验", StringComparison.Ordinal);

        var searchText = hashSectionIndex >= 0 ? body[hashSectionIndex..] : body;
        var entries = Regex.Matches(searchText, @"^\|\s*(?<filename>[^|\r\n]+?)\s*\|\s*(?<sha256>[^|\r\n]+?)\s*\|\s*\r?$", RegexOptions.Multiline)
            .Select(m => (
                fileName: Path.GetFileName(m.Groups["filename"].Value.Trim().Trim('`')),
                sha256: m.Groups["sha256"].Value.Trim().Trim('`')))
            .Where(e => !string.IsNullOrEmpty(e.fileName) && Regex.IsMatch(e.sha256, @"^[0-9a-fA-F]{64}$"))
            .ToList();

        if (entries.Count == 0 && hashSectionIndex >= 0)
        {
            entries = Regex.Matches(body, @"^\|\s*(?<filename>[^|\r\n]+?)\s*\|\s*(?<sha256>[^|\r\n]+?)\s*\|\s*\r?$", RegexOptions.Multiline)
                .Select(m => (
                    fileName: Path.GetFileName(m.Groups["filename"].Value.Trim().Trim('`')),
                    sha256: m.Groups["sha256"].Value.Trim().Trim('`')))
                .Where(e => !string.IsNullOrEmpty(e.fileName) && Regex.IsMatch(e.sha256, @"^[0-9a-fA-F]{64}$"))
                .ToList();
        }

        if (keyword != null)
            return entries.First(e => e.fileName.Contains(keyword, StringComparison.OrdinalIgnoreCase));

        return entries.FirstOrDefault(e =>
                   e.fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) ||
                   e.fileName.EndsWith(".7z", StringComparison.OrdinalIgnoreCase))
               is { fileName: not null } archiveEntry
            ? archiveEntry
            : entries.First();
    }

    public static string GetLatestReleaseContent()
    {
        var sb = new StringBuilder();
        foreach (string l in (LatestRelease.body ?? string.Empty).GetLines(false).SkipWhile(l => l.FirstOrDefault() != '#'))
        {
            if (l.Contains("校验和") ||
                l.StartsWith("## SHA256", StringComparison.OrdinalIgnoreCase) ||
                l.StartsWith("## 下载与启动", StringComparison.Ordinal))
                break;

            sb.AppendLine(l);
        }

        return sb.ToString();
    }

    private static Release GetLatestRelease(IEnumerable<Release> releases, bool isPreRelease)
    {
        if (!isPreRelease)
            releases = releases.Where(release => !release.prerelease);

        var ordered = releases.OrderByDescending(release => release.tag_name, new VersionUtil.VersionComparer());
        return ordered.ElementAt(0);
    }
}
