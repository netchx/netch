using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Netch.Services;

public static class GitHubRateLimitPolicy
{
    public static readonly TimeSpan DefaultBackoff = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan MinBackoff = TimeSpan.FromSeconds(30);
    public static readonly TimeSpan MaxBackoff = TimeSpan.FromHours(2);

    public static bool ShouldRunAutoCheck(int intervalHours, DateTimeOffset? lastAutoCheckUtc, DateTimeOffset nowUtc)
    {
        if (intervalHours <= 0)
            return false;

        if (lastAutoCheckUtc == null)
            return true;

        return nowUtc - lastAutoCheckUtc.Value >= TimeSpan.FromHours(intervalHours);
    }

    public static bool TryParseRateLimitReset(
        HttpStatusCode statusCode,
        HttpResponseHeaders? headers,
        DateTimeOffset nowUtc,
        out DateTimeOffset resetUtc)
    {
        resetUtc = default;
        var isLimitStatus = statusCode is HttpStatusCode.Forbidden or HttpStatusCode.TooManyRequests;
        var remainingExhausted = false;

        if (headers != null &&
            headers.TryGetValues("X-RateLimit-Remaining", out var remainingValues) &&
            remainingValues.FirstOrDefault() is { } remainingRaw &&
            long.TryParse(remainingRaw.Trim(), out var remaining) &&
            remaining <= 0)
        {
            remainingExhausted = true;
        }

        if (!isLimitStatus && !remainingExhausted)
            return false;

        DateTimeOffset? candidate = null;
        if (headers?.RetryAfter is { } retryAfter)
        {
            if (retryAfter.Delta is { } delta)
                candidate = nowUtc + delta;
            else if (retryAfter.Date is { } date)
                candidate = date.ToUniversalTime();
        }

        if (candidate == null &&
            headers != null &&
            headers.TryGetValues("X-RateLimit-Reset", out var resetValues) &&
            resetValues.FirstOrDefault() is { } resetRaw &&
            long.TryParse(resetRaw.Trim(), out var epochSeconds) &&
            epochSeconds > 0)
        {
            candidate = DateTimeOffset.FromUnixTimeSeconds(epochSeconds);
        }

        var rawTarget = candidate ?? nowUtc + DefaultBackoff;
        var minTarget = nowUtc + MinBackoff;
        var maxTarget = nowUtc + MaxBackoff;
        resetUtc = rawTarget < minTarget ? minTarget : rawTarget > maxTarget ? maxTarget : rawTarget;
        return true;
    }

    public static IReadOnlyList<string> GetApplicationAssetCandidates(string? tag, bool selfContained)
    {
        var rawTag = (tag ?? string.Empty).Trim();
        var cleanTag = rawTag.TrimStart('v', 'V');
        if (cleanTag.Length == 0)
            return [];

        var tags = string.Equals(rawTag, cleanTag, StringComparison.Ordinal)
            ? [cleanTag]
            : new[] { cleanTag, rawTag };

        var result = new List<string>(tags.Length * 3);
        foreach (var t in tags)
        {
            if (selfContained)
            {
                result.Add($"Netch-{t}-winui-x64-self-contained.zip");
                result.Add($"Netch-{t}-winui-x64.zip");
            }
            else
            {
                result.Add($"Netch-{t}-winui-x64-framework-dependent.zip");
                result.Add($"Netch-{t}-winui-x64-self-contained.zip");
                result.Add($"Netch-{t}-winui-x64.zip");
            }
        }

        return result;
    }
}

public sealed class CachedApiResponse
{
    public string? ETag { get; set; }

    public string Body { get; set; } = string.Empty;

    public DateTimeOffset FetchedAtUtc { get; set; }
}

public sealed class CachedUpdateEntry
{
    public string Version { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public string ReleaseUrl { get; set; } = string.Empty;

    public bool Prerelease { get; set; }

    public DateTimeOffset CheckedAtUtc { get; set; }
}

public sealed class CachedCoreHistory
{
    public bool IncludePrerelease { get; set; }

    public DateTimeOffset FetchedAtUtc { get; set; }

    public List<CachedUpdateEntry> Entries { get; set; } = [];
}

public sealed class CachedAppReleaseEntry
{
    public string TagName { get; set; } = string.Empty;

    public string HtmlUrl { get; set; } = string.Empty;

    public bool Prerelease { get; set; }

    public string? AssetName { get; set; }

    public string? AssetDownloadUrl { get; set; }

    public DateTimeOffset CheckedAtUtc { get; set; }
}

public sealed class UpdateCacheState
{
    public DateTimeOffset? LastAutoCheckUtc { get; set; }

    public DateTimeOffset? LastCheckedUtc { get; set; }

    public DateTimeOffset? RateLimitResetUtc { get; set; }

    public Dictionary<string, CachedApiResponse> ApiResponses { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, CachedUpdateEntry> CoreLatest { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, CachedCoreHistory> CoreHistory { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public CachedAppReleaseEntry? AppRelease { get; set; }

    public string? RemotePacVersion { get; set; }

    public string? RulesSourceKey { get; set; }

    public string? RulesTreeSha { get; set; }

    public int RemoteRuleCount { get; set; }
}

public sealed class UpdateCacheStore(string filePath)
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly object _sync = new();

    public string FilePath { get; } = filePath;

    public UpdateCacheState Load()
    {
        lock (_sync)
        {
            if (!File.Exists(FilePath))
                return new UpdateCacheState();

            try
            {
                var json = File.ReadAllText(FilePath);
                var state = JsonSerializer.Deserialize<UpdateCacheState>(json, SerializerOptions) ?? new UpdateCacheState();
                state.ApiResponses = new Dictionary<string, CachedApiResponse>(state.ApiResponses ?? [], StringComparer.OrdinalIgnoreCase);
                state.CoreLatest = new Dictionary<string, CachedUpdateEntry>(state.CoreLatest ?? [], StringComparer.OrdinalIgnoreCase);
                state.CoreHistory = new Dictionary<string, CachedCoreHistory>(state.CoreHistory ?? [], StringComparer.OrdinalIgnoreCase);
                return state;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
            {
                Log.Warning(ex, "Read update cache {Path} failed", FilePath);
                return new UpdateCacheState();
            }
        }
    }

    public void Save(UpdateCacheState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        lock (_sync)
        {
            var dir = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            var tmp = FilePath + ".tmp";
            try
            {
                File.WriteAllBytes(tmp, JsonSerializer.SerializeToUtf8Bytes(state, SerializerOptions));
                File.Move(tmp, FilePath, overwrite: true);
            }
            finally
            {
                if (File.Exists(tmp))
                    File.Delete(tmp);
            }
        }
    }
}
