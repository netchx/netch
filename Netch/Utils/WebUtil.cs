using Microsoft.VisualStudio.Threading;
using System.Net;
using System.Text;

namespace Netch.Utils;

public static class WebUtil
{
    public const string DefaultUserAgent = @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/94.0.4606.61 Safari/537.36 Edg/94.0.992.31";
    private static readonly HttpClient _httpClient = CreateHttpClient();


    static WebUtil()
    {
        ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
    }


    private static int DefaultGetTimeout => Global.Settings.RequestTimeout;

    public static HttpClient CreateHttpClient(int? timeout = null, string? userAgent = null, string? proxyServer = null)
    {
        var handler = new HttpClientHandler
        {
            // 自动解压（以前 WebRequest 默认没开）
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            UseProxy = !string.IsNullOrWhiteSpace(proxyServer),
            Proxy = !string.IsNullOrEmpty(proxyServer) ? new WebProxy(proxyServer) : null
        };

        var client = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromMilliseconds(timeout ?? DefaultGetTimeout)
        };

        client.DefaultRequestHeaders.UserAgent.ParseAdd(string.IsNullOrWhiteSpace(userAgent) ? DefaultUserAgent : userAgent);

        client.DefaultRequestHeaders.Accept.ParseAdd("*/*");
        client.DefaultRequestHeaders.AcceptCharset.ParseAdd("utf-8");
        client.DefaultRequestHeaders.Connection.ParseAdd("Keep-Alive");
        return client;
    }

    public static async Task<byte[]> DownloadBytesAsync(string address)
    {
        using var response = await _httpClient.GetAsync(address);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync();
    }

    public static async Task<(HttpStatusCode, string)> DownloadStringAsync(string address, string? userAgent = null, string? proxyServer = null, Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;
        // 判断是否需要创建临时客户端
        bool needsTempClient = !string.IsNullOrWhiteSpace(proxyServer) || !string.IsNullOrWhiteSpace(userAgent);

        HttpClient httpClient;

        if (needsTempClient)
        {
            httpClient = CreateHttpClient(proxyServer: proxyServer, userAgent: userAgent);
        }
        else
        {
            // 都不需要时，使用全局实例
            httpClient = _httpClient;
        }
        try
        {
            using var response = await httpClient.GetAsync(address, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
            var statusCode = response.StatusCode;
            await using var responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            using var streamReader = new StreamReader(responseStream, encoding);
            var content = await streamReader.ReadToEndAsync().ConfigureAwait(false);
            return (statusCode, content); ;
        }
        finally
        {
            // 如果是临时客户端，手动释放
            if (needsTempClient)
            {
                httpClient.Dispose();
            }
        }
    }

    public static async Task DownloadFileAsync(string address, string fileFullPath, IProgress<int>? progress)
    {
        using var response = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, address), HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        var total = response.Content.Headers.ContentLength ?? -1L;
        await using var input = await response.Content.ReadAsStreamAsync();
        await using var fileStream = new FileStream(fileFullPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true);
        var copyTask = input.CopyToAsync(fileStream);
        if (progress != null && total > 0)
        {
            ReportProgressAsync(total, copyTask, fileStream, progress, 200).Forget();
        }
        await copyTask;
        progress?.Report(100);
    }

    private static async Task ReportProgressAsync(long total, IAsyncResult downloadTask, Stream stream, IProgress<int> progress, int interval)
    {
        var last = 0;
        while (!downloadTask.IsCompleted)
        {
            var current = (int)((double)stream.Length / total * 100);
            if (last != current)
            {
                last = current;
                progress.Report(last);
            }
            await Task.Delay(interval);
        }
    }
}