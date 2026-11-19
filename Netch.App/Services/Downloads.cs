using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Threading;
using Netch.Controllers;
using Netch.Enums;
using Netch.Models.GitHubRelease;
using Netch.Services;
using Netch.Utils;

namespace Netch.App.Services;

internal enum ModeOrigin { Bundled, Update, Modified, Custom }

internal enum RuleChangeKind { Added, Updated, Conflict }
internal enum ConflictAction { Keep, Replace, Copy }

internal sealed record RuleChange(string Path, string Sha, string? LocalSha, RuleChangeKind Kind);

internal sealed record CorePackage(string Name, string Repo, string File, string VersionArgument, Func<string, string> Asset, string? ArchiveFile = null);
internal sealed record UpdateChoice(string Version, string Url, string ReleaseUrl);
internal readonly record struct TransferProgress(double Percent, ulong BytesPerSecond);
internal sealed record DownloadEntry(DateTime Time, string Name, string Version, bool Success, string? Message = null);

internal static partial class Downloads
{
    public static readonly CorePackage[] Cores =
    [
        new("sing-box", "SagerNet/sing-box", @"sing_box\sing-box.exe", "version", v => $"sing-box-{v}-windows-amd64.zip"),
        new("Xray-core", "XTLS/Xray-core", "xray.exe", "version", _ => "Xray-windows-64.zip"),
        new("Mihomo", "MetaCubeX/mihomo", "mihomo.exe", "-v", v => $"mihomo-windows-amd64-v1-v{v}.zip", "mihomo-windows-amd64-v1.exe"),
        new("tun2socks", "xjasonlyu/tun2socks", "tun2socks-windows-amd64-v3.exe", "-version", _ => "tun2socks-windows-amd64-v3.zip"),
        new("WinTUN", "", "wintun.dll", "", v => $"wintun-{v}.zip", "wintun/bin/amd64/wintun.dll"),
        new("pcap2socks", "zhxie/pcap2socks", "pcap2socks.exe", "--version", v => $"pcap2socks-v{v}-windows-amd64.zip")
    ];

    public static event Action? Changed;
    public static Dictionary<CorePackage, List<UpdateChoice>> Versions { get; } = [];
    public static Dictionary<CorePackage, string> Installed { get; } = [];
    public static Dictionary<string, string> Errors { get; } = [];
    public static List<RuleChange> Rules { get; private set; } = [];
    public static int RemoteRuleCount { get; private set; }
    public static string RemoteTree { get; private set; } = "";
    public static DateTime? LastChecked { get; private set; }
    public static DateTimeOffset? LastRulesSynchronized => _manifest.Source == SourceKey ? _manifest.SynchronizedAt : null;
    public static Release? AppRelease { get; private set; }
    public static Asset? AppAsset { get; private set; }
    public static List<DownloadEntry> History { get; } = ReadJson<List<DownloadEntry>>(HistoryPath) ?? [];
    public static bool Checking { get; private set; }
    public static bool Applying { get; private set; }
    public static int PendingCount => Rules.Count + Versions.Count(entry =>
        entry.Value.FirstOrDefault() is { } version && IsNewer(version.Version, Installed.GetValueOrDefault(entry.Key) ?? ""));
    private static string HistoryPath => Path.Combine(Configuration.DataDirectoryFullName, "downloads.json");
    private static string BaseRulesPath => Path.Combine(ModeService.Instance.ModeDirectoryFullName, ModeService.ManifestFileName);
    private static string SourceKey => $"{Global.Settings.ModeRepository}/{Global.Settings.ModeBranch}/{Global.Settings.ModeRepositoryPath}";
    private static RuleManifest _manifest = ReadJson<RuleManifest>(BaseRulesPath) ?? new();
    private sealed record RuleManifest
    {
        public string? Source { get; set; }
        public DateTimeOffset? SynchronizedAt { get; set; }
        public Dictionary<string, string> Blobs { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    }

    public static async Task CheckAsync()
    {
        if (Checking || Applying)
            return;
        Checking = true;
        Errors.Clear();
        Changed?.Invoke();
        try
        {
            var results = await Task.WhenAll(Cores.Select(async core =>
            {
                try { return (Core: core, Version: await ReadVersionAsync(core), Choices: await FetchCoreAsync(core), Error: (string?)null); }
                catch (Exception e) { return (Core: core, Version: await ReadVersionAsync(core), Choices: new List<UpdateChoice>(), Error: (string?)e.Message); }
            }));
            foreach (var result in results)
            {
                Installed[result.Core] = result.Version;
                Versions[result.Core] = result.Choices;
                if (result.Error != null) Errors[result.Core.Name] = result.Error;
            }
            await Task.WhenAll(CheckRulesAsync(), CheckApplicationAsync());
            LastChecked = DateTime.Now;
        }
        finally
        {
            Checking = false;
            Changed?.Invoke();
        }
    }

    private static async Task CheckApplicationAsync()
    {
        try
        {
            var releases = await ReleasesAsync($"{UpdateChecker.Owner}/{UpdateChecker.Repo}");
            AppRelease = releases.FirstOrDefault();
            AppAsset = AppRelease?.assets?.FirstOrDefault(asset => asset.name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)
                && asset.name.Contains("winui", StringComparison.OrdinalIgnoreCase) && asset.name.Contains("x64", StringComparison.OrdinalIgnoreCase));
            if (AppRelease != null && IsNewer(AppRelease.tag_name, UpdateChecker.Version) && AppAsset == null)
                Errors[UpdateChecker.Name] = Helpers.Ui.T("This release has no WinUI x64 ZIP.", "该版本未提供 WinUI x64 ZIP。");
        }
        catch (Exception e) { Errors[UpdateChecker.Name] = e.Message; }
    }

    public static bool IsNewer(string available, string installed)
    {
        if (available.Length == 0)
            return false;
        if (installed.Length == 0)
            return true;
        return VersionUtil.CompareVersion(available.TrimStart('v', 'V'), installed.TrimStart('v', 'V')) > 0;
    }

    public static async Task PopulateInstalledAsync()
    {
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
                { UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true });
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
            return VersionNumber().Match(version ?? "").Value;
        }
        catch (Exception e)
        {
            Log.Debug(e, "Read {Name} version", core.Name);
            return "";
        }
    }

    [GeneratedRegex(@"\d+\.\d+\.\d+(?:-[A-Za-z]+\.?\d*)?")]
    private static partial Regex VersionNumber();

    private static async Task<List<UpdateChoice>> FetchCoreAsync(CorePackage core)
    {
        if (core.Repo.Length == 0)
        {
            using var client = CreateClient();
            var page = await client.GetStringAsync("https://www.wintun.net/");
            return Regex.Matches(page, @"(?:https://www\.wintun\.net)?/?builds/wintun-(?<version>\d+\.\d+\.\d+)\.zip")
                .Select(match => match.Groups["version"].Value).Distinct()
                .OrderByDescending(version => Version.Parse(version))
                .Select(version => new UpdateChoice(version, $"https://www.wintun.net/builds/{core.Asset(version)}", "https://www.wintun.net/"))
                .ToList();
        }

        return (await ReleasesAsync(core.Repo)).Select(release =>
        {
            var version = release.tag_name.TrimStart('v', 'V');
            var name = core.Asset(version);
            var asset = release.assets?.FirstOrDefault(a => string.Equals(a.name, name, StringComparison.OrdinalIgnoreCase));
            return asset == null ? null : new UpdateChoice(version, asset.browser_download_url, release.html_url);
        }).OfType<UpdateChoice>().ToList();
    }

    private static async Task<List<Release>> ReleasesAsync(string repo)
    {
        using var client = CreateClient();
        var json = await client.GetStringAsync(Request($"https://api.github.com/repos/{repo}/releases?per_page=20"));
        return (JsonSerializer.Deserialize<List<Release>>(json) ?? [])
            .Where(r => !r.draft && (Global.Settings.CheckBetaUpdate || !r.prerelease)).ToList();
    }

    private static HttpClient CreateClient()
    {
        string? proxy = null;
        if (Global.Settings.DownloadChannel == DownloadChannel.Proxy)
        {
            var server = MainController.Socks5Server;
            if (server == null)
                throw new InvalidOperationException(Helpers.Ui.T("Connect a scene to use the current proxy.", "请先连接场景，再通过当前代理下载。"));
            var uri = new UriBuilder("socks5", server.Address is "0.0.0.0" or "::" ? "127.0.0.1" : server.Address, server.Port);
            if (server.Auth()) { uri.UserName = server.Username; uri.Password = server.Password; }
            proxy = uri.Uri.AbsoluteUri;
        }
        return WebUtil.CreateHttpClient(proxyServer: proxy);
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

    private static async Task CheckRulesAsync()
    {
        try
        {
            using var client = CreateClient();
            var branch = Uri.EscapeDataString(Global.Settings.ModeBranch);
            var url = $"https://api.github.com/repos/{Global.Settings.ModeRepository}/git/trees/{branch}?recursive=1";
            using var response = JsonDocument.Parse(await client.GetStringAsync(Request(url)));
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
        catch (Exception e) { Errors["Rules"] = e.Message; }
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
        Rules = [];
        RemoteRuleCount = 0;
        RemoteTree = "";
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
        "WinTUN" => MainController.ModeController is TUNController,
        _ => core.Name.Equals(MainController.ServerController?.Name, StringComparison.OrdinalIgnoreCase) ||
             core.Name.Equals(MainController.ModeController?.Name, StringComparison.OrdinalIgnoreCase)
    });

    public static string CorePath(CorePackage core) => Path.Combine(Bootstrap.BinDirectory, core.File);

    public static async Task InstallAsync(CorePackage core, UpdateChoice version, IProgress<TransferProgress> progress, Func<CancellationToken, Task> wait, CancellationToken token)
    {
        if (Applying || Checking)
            return;
        Applying = true;
        try
        {
            using var client = CreateClient();
            using var response = await client.GetAsync(Request(version.Url), HttpCompletionOption.ResponseHeadersRead, token);
            response.EnsureSuccessStatusCode();
            using var archiveFile = File.Create(Path.GetTempFileName(), 81920, FileOptions.DeleteOnClose);
            await using (var source = await response.Content.ReadAsStreamAsync(token))
            {
                await TransferAsync(source, archiveFile, response.Content.Headers.ContentLength, progress, wait, token);
            }
            archiveFile.Position = 0;
            using var zip = new ZipArchive(archiveFile, ZipArchiveMode.Read);
            var fileName = core.ArchiveFile ?? Path.GetFileName(core.File);
            var entry = zip.Entries.SingleOrDefault(e => core.ArchiveFile?.Contains('/') == true ? e.FullName == fileName : e.Name == fileName)
                ?? throw new InvalidDataException($"The archive is missing {fileName}.");
            var path = CorePath(core);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var staged = path + ".download";
            try
            {
                entry.ExtractToFile(staged, overwrite: true);
                token.ThrowIfCancellationRequested();
                await ReplaceCoreAsync(core, path, staged);
            }
            finally { File.Delete(staged); }

            progress.Report(new TransferProgress(100, 0));
            Record(core.Name, version.Version, true);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { Record(core.Name, version.Version, false, "Paused"); }
        catch (Exception e)
        {
            Record(core.Name, version.Version, false, e.Message);
            throw;
        }
        finally
        {
            Applying = false;
            Changed?.Invoke();
        }
    }

    private static async Task ReplaceCoreAsync(CorePackage core, string path, string staged)
    {
        var session = Session.Current;
        var scene = InUse(core) ? session.ActiveScene : null;
        var backup = path + ".download-backup";
        var hadCore = File.Exists(path);
        if (scene != null)
            await session.StopAsync();

        var replaced = false;
        try
        {
            if (hadCore)
                File.Replace(staged, path, backup);
            else
                File.Move(staged, path);
            replaced = true;
            Installed[core] = await ReadVersionAsync(core);
            if (Installed[core].Length == 0)
                throw new InvalidDataException(Helpers.Ui.T("The replacement core could not report its version.",
                    "替换后的核心无法报告版本。"));
            if (scene != null)
            {
                await session.StartAsync(scene);
                if (!session.IsRunning || session.ActiveScene != scene)
                    throw new InvalidDataException(Helpers.Ui.T("The updated core could not reconnect the scene.",
                        "更新后的核心未能重新连接场景。") + " " + session.ErrorMessage);
            }
            if (hadCore)
                File.Move(backup, path + ".previous", overwrite: true);
        }
        catch
        {
            if (replaced)
            {
                if (scene != null && session.IsRunning && session.ActiveScene == scene)
                    await session.StopAsync();
                if (hadCore)
                    File.Replace(backup, path, null);
                else
                    File.Delete(path);
                Installed[core] = await ReadVersionAsync(core);
            }
            if (scene != null)
                await session.StartAsync(scene);
            throw;
        }
    }

    public static async Task RollbackAsync(CorePackage core)
    {
        if (Applying || Checking)
            return;
        Applying = true;
        var path = CorePath(core);
        var staged = path + ".download";
        try
        {
            Changed?.Invoke();
            File.Copy(path + ".previous", staged, overwrite: true);
            await ReplaceCoreAsync(core, path, staged);
            Record(core.Name, Installed[core], true, "Rollback");
        }
        finally
        {
            Applying = false;
            File.Delete(staged);
            Changed?.Invoke();
        }
    }

    public static async Task UpdateApplicationAsync(IProgress<TransferProgress> progress, Func<CancellationToken, Task> wait, CancellationToken token)
    {
        if (Applying || Checking)
            return;
        Applying = true;
        try
        {
            Changed?.Invoke();
            await AppUpdate.RestartAsync(await DownloadApplicationAsync(progress, wait, token));
        }
        finally
        {
            Applying = false;
            Changed?.Invoke();
        }
    }

    private static async Task<string> DownloadApplicationAsync(IProgress<TransferProgress> progress, Func<CancellationToken, Task> wait, CancellationToken token)
    {
        var asset = AppAsset ?? throw new InvalidOperationException("This release has no WinUI x64 ZIP.");
        var file = Path.Combine(Path.GetTempPath(), $"Netch-update-{Guid.NewGuid():N}.zip");
        using var client = CreateClient();
        using var response = await client.GetAsync(Request(asset.browser_download_url), HttpCompletionOption.ResponseHeadersRead, token);
        response.EnsureSuccessStatusCode();
        try
        {
            await using var target = File.Create(file);
            await using var source = await response.Content.ReadAsStreamAsync(token);
            await TransferAsync(source, target, response.Content.Headers.ContentLength, progress, wait, token);
            Record(UpdateChecker.Name, AppRelease!.tag_name, true);
            return file;
        }
        catch
        {
            File.Delete(file);
            throw;
        }
    }

    private static async Task TransferAsync(Stream source, Stream target, long? total,
        IProgress<TransferProgress> progress, Func<CancellationToken, Task> wait, CancellationToken token)
    {
        var buffer = new byte[81920];
        var reported = int.MinValue;
        ulong speed = 0;
        var transferred = 0L;
        var sample = Stopwatch.StartNew();
        while (await source.ReadAsync(buffer, token) is var count && count > 0)
        {
            sample.Stop();
            await wait(token);
            sample.Start();
            await target.WriteAsync(buffer.AsMemory(0, count), token);
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
                progress.Report(new TransferProgress(percent, speed));
            }
        }
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
