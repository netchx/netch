using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Netch.Models;
using Netch.Servers;

namespace Netch.Utils;

public static class CoreDelayTester
{
    private const int MaxDrainBytes = 64 * 1024;
    private static readonly TimeSpan CoreStartTimeout = TimeSpan.FromSeconds(10);

    internal static Func<CoreSelector.CoreKind, string>? CoreExecutableResolver { get; set; }

    public static async Task<int> ProbeOverSocks5Async(
        string proxyHost,
        ushort proxyPort,
        string? username,
        string? password,
        Uri targetUri,
        int timeoutMs,
        bool steadySecondRequest,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var clampedTimeout = Math.Clamp(timeoutMs, Constants.MinDelayTestTimeout, Constants.MaxDelayTestTimeout);
        var connectHost = NormalizeLoopbackHost(proxyHost);
        var proxy = new WebProxy($"socks5://{ProxyUri.Host(connectHost)}:{proxyPort}");
        if (!string.IsNullOrEmpty(username) || !string.IsNullOrEmpty(password))
            proxy.Credentials = new NetworkCredential(username ?? string.Empty, password ?? string.Empty);

        using var handler = new SocketsHttpHandler
        {
            Proxy = proxy,
            UseProxy = true,
            AllowAutoRedirect = false,
            UseCookies = false,
            AutomaticDecompression = DecompressionMethods.None,
            ConnectTimeout = TimeSpan.FromMilliseconds(clampedTimeout),
            MaxConnectionsPerServer = 1,
            PooledConnectionLifetime = TimeSpan.FromMinutes(1)
        };

        using var client = new HttpClient(handler, disposeHandler: true)
        {
            Timeout = Timeout.InfiniteTimeSpan
        };

        async Task<int> SendOnceAsync()
        {
            using var reqTimeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            reqTimeoutCts.CancelAfter(clampedTimeout);

            using var request = new HttpRequestMessage(HttpMethod.Get, targetUri)
            {
                Version = HttpVersion.Version11,
                VersionPolicy = HttpVersionPolicy.RequestVersionOrLower
            };
            request.Headers.ConnectionClose = false;

            var sw = Stopwatch.StartNew();
            try
            {
                using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, reqTimeoutCts.Token);
                sw.Stop();

                var statusCode = (int)response.StatusCode;
                if (statusCode is < 200 or >= 400)
                    return DelayStatus.Unreachable;

                if (steadySecondRequest)
                    await DrainResponseAsync(response.Content, reqTimeoutCts.Token);

                return Convert.ToInt32(sw.Elapsed.TotalMilliseconds);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException)
            {
                return DelayStatus.Timeout;
            }
            catch (HttpRequestException ex)
            {
                return HasTimedOutSocketError(ex) ? DelayStatus.Timeout : DelayStatus.Unreachable;
            }
            catch (SocketException ex)
            {
                return ex.SocketErrorCode == SocketError.TimedOut ? DelayStatus.Timeout : DelayStatus.Unreachable;
            }
            catch (IOException ex)
            {
                return HasTimedOutSocketError(ex) ? DelayStatus.Timeout : DelayStatus.Unreachable;
            }
        }

        var first = await SendOnceAsync();
        if (!steadySecondRequest || !DelayStatus.IsValid(first))
            return first;

        var second = await SendOnceAsync();
        return DelayStatus.IsValid(second) ? second : first;
    }

    internal static async Task TestBatchAsync(
        IReadOnlyList<(Server Server, IPAddress PrimaryIp)> targets,
        Uri testUri,
        int timeoutMs,
        bool steadySecondRequest,
        SemaphoreSlim globalConcurrency,
        Action<Server, int> onResult,
        CancellationToken ct,
        Func<Server, int?>? missingCoreFallback = null)
    {
        if (targets.Count == 0)
            return;

        var buckets = targets
            .GroupBy(item => CoreSelector.SelectCore(item.Server))
            .Select(group => RunCoreBucketAsync(
                group.Key,
                group.ToList(),
                testUri,
                timeoutMs,
                steadySecondRequest,
                globalConcurrency,
                onResult,
                missingCoreFallback,
                ct));

        await Task.WhenAll(buckets);
    }

    private static async Task RunCoreBucketAsync(
        CoreSelector.CoreKind coreKind,
        List<(Server Server, IPAddress PrimaryIp)> items,
        Uri testUri,
        int timeoutMs,
        bool steadySecondRequest,
        SemaphoreSlim globalConcurrency,
        Action<Server, int> onResult,
        Func<Server, int?>? missingCoreFallback,
        CancellationToken ct)
    {
        var exePath = GetCoreExecutablePath(coreKind);
        if (!File.Exists(exePath))
        {
            Log.Warning("Delay test skipped for {Core}: executable not found at {Path}", coreKind, exePath);
            foreach (var (server, _) in items)
                onResult(server, missingCoreFallback?.Invoke(server) ?? DelayStatus.Unreachable);
            return;
        }

        await RunCoreSliceAsync(coreKind, exePath, items, testUri, timeoutMs, steadySecondRequest, globalConcurrency, onResult, ct);
    }

    private static async Task RunCoreSliceAsync(
        CoreSelector.CoreKind coreKind,
        string exePath,
        IReadOnlyList<(Server Server, IPAddress PrimaryIp)> items,
        Uri testUri,
        int timeoutMs,
        bool steadySecondRequest,
        SemaphoreSlim globalConcurrency,
        Action<Server, int> onResult,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var secret = Convert.ToHexString(RandomNumberGenerator.GetBytes(12)).ToLowerInvariant();
        var port = AllocateLoopbackPort();
        var validEntries = new List<(string Tag, Server Server, IPAddress PrimaryIp)>();
        object? configObject = null;

        switch (coreKind)
        {
            case CoreSelector.CoreKind.Mihomo:
            {
                var built = new List<(string Tag, Dictionary<string, object?> Proxy)>();
                for (var i = 0; i < items.Count; i++)
                {
                    var (server, ip) = items[i];
                    var tag = $"n{i}";
                    try
                    {
                        var proxy = MihomoConfig.BuildProxy(server, ip.ToString(), tag);
                        built.Add((tag, proxy));
                        validEntries.Add((tag, server, ip));
                    }
                    catch (Exception ex)
                    {
                        Log.Debug(ex, "Skipping Mihomo node {Node} during delay test", server);
                        onResult(server, DelayStatus.Unreachable);
                    }
                }
                if (built.Count > 0)
                    configObject = MihomoConfig.BuildBatchProbeConfig(built, port, secret);
                break;
            }
            case CoreSelector.CoreKind.Singbox:
            {
                var built = new List<(string Tag, Server Server, Dictionary<string, object?> Proxy)>();
                for (var i = 0; i < items.Count; i++)
                {
                    var (server, ip) = items[i];
                    var tag = $"n{i}";
                    try
                    {
                        var proxy = SingboxController.BuildProxy(server, tag);
                        built.Add((tag, server, proxy));
                        validEntries.Add((tag, server, ip));
                    }
                    catch (Exception ex)
                    {
                        Log.Debug(ex, "Skipping sing-box node {Node} during delay test", server);
                        onResult(server, DelayStatus.Unreachable);
                    }
                }
                if (built.Count > 0)
                    configObject = SingboxController.BuildBatchProbeConfig(built, port, secret);
                break;
            }
            case CoreSelector.CoreKind.Xray:
            {
                var built = new List<(string Tag, Server Server, Outbounds4Ray Outbound)>();
                for (var i = 0; i < items.Count; i++)
                {
                    var (server, ip) = items[i];
                    var tag = $"n{i}";
                    try
                    {
                        var outbound = V2rayConfigUtils.GenerateOutbound(server, tag);
                        built.Add((tag, server, outbound));
                        validEntries.Add((tag, server, ip));
                    }
                    catch (Exception ex)
                    {
                        Log.Debug(ex, "Skipping Xray node {Node} during delay test", server);
                        onResult(server, DelayStatus.Unreachable);
                    }
                }
                if (built.Count > 0)
                    configObject = V2rayConfigUtils.GenerateBatchProbeConfig(built, port, secret);
                break;
            }
        }

        if (configObject == null || validEntries.Count == 0)
            return;

        var dataDir = Path.Combine(Global.NetchDir, "data");
        var binDir = Path.Combine(Global.NetchDir, "bin");
        Directory.CreateDirectory(dataDir);
        var configPath = Path.Combine(dataDir, $"probe-{coreKind.ToString().ToLowerInvariant()}-{Guid.NewGuid():N}.json");

        var needBisect = false;
        Process? process = null;
        try
        {
            await using (var fs = new FileStream(configPath, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                await JsonSerializer.SerializeAsync(fs, configObject, Global.NewCustomJsonSerializerOptions(), ct);
            }

            var arguments = coreKind switch
            {
                CoreSelector.CoreKind.Mihomo => $"-d \"{dataDir}\" -f \"{configPath}\"",
                CoreSelector.CoreKind.Singbox => $"run --disable-color -c \"{configPath}\"",
                CoreSelector.CoreKind.Xray => $"run -c \"{configPath}\"",
                _ => throw new ArgumentOutOfRangeException(nameof(coreKind))
            };

            process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = arguments,
                    WorkingDirectory = Directory.Exists(binDir) ? binDir : Path.GetDirectoryName(exePath) ?? Global.NetchDir,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8,
                    WindowStyle = ProcessWindowStyle.Hidden
                }
            };

            process.Start();
            try
            {
                Global.Job.AddProcess(process);
            }
            catch
            {
                // Ignore if the short-lived process already exited.
            }

            var started = await WaitForCoreReadyAsync(coreKind, process, ct);
            if (!started)
            {
                needBisect = true;
            }
            else
            {
                var probeTasks = validEntries.Select(async entry =>
                {
                    await globalConcurrency.WaitAsync(ct);
                    try
                    {
                        var delay = await ProbeOverSocks5Async("127.0.0.1", port, entry.Tag, secret, testUri, timeoutMs, steadySecondRequest, ct);
                        onResult(entry.Server, delay);
                    }
                    catch (OperationCanceledException) when (ct.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        Log.Debug(ex, "Real-connection probe failed for {Node}", entry.Server);
                        onResult(entry.Server, DelayStatus.Unreachable);
                    }
                    finally
                    {
                        globalConcurrency.Release();
                    }
                });

                await Task.WhenAll(probeTasks);
            }
        }
        finally
        {
            if (process != null)
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill(entireProcessTree: true);
                        await process.WaitForExitAsync();
                    }
                }
                catch
                {
                    // Best-effort cleanup of probe child process.
                }
                process.Dispose();
            }

            try
            {
                if (File.Exists(configPath))
                    File.Delete(configPath);
            }
            catch
            {
                // Best-effort cleanup of temp config file.
            }
        }

        if (needBisect)
        {
            ct.ThrowIfCancellationRequested();
            if (validEntries.Count == 1)
            {
                onResult(validEntries[0].Server, DelayStatus.Unreachable);
                return;
            }

            var mid = validEntries.Count / 2;
            var left = validEntries.Take(mid).Select(e => (e.Server, e.PrimaryIp)).ToList();
            var right = validEntries.Skip(mid).Select(e => (e.Server, e.PrimaryIp)).ToList();
            Log.Information("Bisecting {Core} probe batch ({Count} nodes) after startup failure", coreKind, validEntries.Count);
            await RunCoreSliceAsync(coreKind, exePath, left, testUri, timeoutMs, steadySecondRequest, globalConcurrency, onResult, ct);
            await RunCoreSliceAsync(coreKind, exePath, right, testUri, timeoutMs, steadySecondRequest, globalConcurrency, onResult, ct);
        }
    }

    private static async Task<bool> WaitForCoreReadyAsync(CoreSelector.CoreKind coreKind, Process process, CancellationToken ct)
    {
        var readyKeyword = coreKind switch
        {
            CoreSelector.CoreKind.Mihomo => "SOCKS[probe] proxy listening at:",
            CoreSelector.CoreKind.Singbox => "sing-box started",
            CoreSelector.CoreKind.Xray => "started",
            _ => "started"
        };

        string[] failKeywords = coreKind switch
        {
            CoreSelector.CoreKind.Mihomo => ["level=fatal", "Listener probe listen err:"],
            CoreSelector.CoreKind.Singbox => ["FATAL"],
            CoreSelector.CoreKind.Xray => ["config file not readable", "failed to", "Failed to start"],
            _ => ["FATAL"]
        };

        var readyTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var ring = new Queue<string>(32);
        var ringLock = new object();

        async Task PumpAsync(TextReader reader)
        {
            try
            {
                string? line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    lock (ringLock)
                    {
                        if (ring.Count >= 32)
                            ring.Dequeue();
                        ring.Enqueue(line);
                    }

                    if (!readyTcs.Task.IsCompleted)
                    {
                        if (line.Contains(readyKeyword, StringComparison.Ordinal))
                            readyTcs.TrySetResult(true);
                        else if (failKeywords.Any(k => line.Contains(k, StringComparison.Ordinal)))
                            readyTcs.TrySetResult(false);
                    }
                }
            }
            catch (Exception e) when (e is ObjectDisposedException or IOException or InvalidOperationException)
            {
                // Process closed while reading output.
            }
        }

        var outPump = PumpAsync(process.StandardOutput);
        var errPump = PumpAsync(process.StandardError);

        using var startupCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        startupCts.CancelAfter(CoreStartTimeout);

        try
        {
            var exitTask = process.WaitForExitAsync(startupCts.Token);
            var delayTask = Task.Delay(CoreStartTimeout, startupCts.Token);
            var finished = await Task.WhenAny(readyTcs.Task, exitTask, delayTask);
            ct.ThrowIfCancellationRequested();

            if (finished == readyTcs.Task && await readyTcs.Task)
                return true;

            if (finished == exitTask)
            {
                // Let pumps flush any trailing output briefly before deciding failure.
                await Task.WhenAny(Task.WhenAll(outPump, errPump), Task.Delay(100, ct));
                if (readyTcs.Task.IsCompleted && await readyTcs.Task)
                    return true;
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            // Startup timeout.
        }

        string tail;
        lock (ringLock)
        {
            tail = string.Join(Environment.NewLine, ring);
        }
        Log.Warning("Probe core {Core} failed to become ready. Output:\n{Output}", coreKind, tail);
        return false;
    }

    private static bool HasTimedOutSocketError(Exception ex)
    {
        for (Exception? current = ex; current != null; current = current.InnerException)
        {
            if (current is SocketException sockEx && sockEx.SocketErrorCode == SocketError.TimedOut)
                return true;
        }

        return false;
    }

    private static string GetCoreExecutablePath(CoreSelector.CoreKind coreKind)
    {
        if (CoreExecutableResolver != null)
            return CoreExecutableResolver(coreKind);

        return coreKind switch
        {
            CoreSelector.CoreKind.Mihomo => Path.Combine(Global.NetchDir, "bin", "mihomo.exe"),
            CoreSelector.CoreKind.Singbox => SingboxController.FindExecutable(),
            CoreSelector.CoreKind.Xray => Path.Combine(Global.NetchDir, "bin", "xray.exe"),
            _ => throw new ArgumentOutOfRangeException(nameof(coreKind))
        };
    }

    private static ushort AllocateLoopbackPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        try
        {
            return (ushort)((IPEndPoint)listener.LocalEndpoint).Port;
        }
        finally
        {
            listener.Stop();
        }
    }

    private static string NormalizeLoopbackHost(string host)
    {
        if (IPAddress.TryParse(host, out var ip))
        {
            if (ip.Equals(IPAddress.Any))
                return IPAddress.Loopback.ToString();
            if (ip.Equals(IPAddress.IPv6Any))
                return IPAddress.IPv6Loopback.ToString();
        }

        return host;
    }

    private static async Task DrainResponseAsync(HttpContent content, CancellationToken ct)
    {
        try
        {
            await using var stream = await content.ReadAsStreamAsync(ct);
            var buffer = new byte[4096];
            var total = 0;
            while (total < MaxDrainBytes)
            {
                var read = await stream.ReadAsync(buffer.AsMemory(0, Math.Min(buffer.Length, MaxDrainBytes - total)), ct);
                if (read == 0)
                    break;
                total += read;
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            // Ignore body drain errors; the status headers were already received.
        }
    }
}
